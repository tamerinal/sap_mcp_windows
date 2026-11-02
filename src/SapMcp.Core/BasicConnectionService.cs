using System.Text.Json;
using System.Text.RegularExpressions;

namespace SapMcp.Core;

public enum BasicPrompt { ConnectionWarning,RfcDiscovery,AutomaticScan,ExtendedScan }
public sealed record BasicValidationResult(AppSettings? Settings,BasicCheck Adt,BasicCheck Rfc,BasicDiscovery Discovery,string Url="");
internal sealed class BasicValidationHooks
{
    internal Func<AppSettings,string,string,CancellationToken,Task<BasicCheck>>? Check {get;init;}
    internal Func<AppSettings,string,string,CancellationToken,Task<BasicRfcServers>>? Servers {get;init;}
    internal Func<AppSettings,BasicDraft,string,IReadOnlyList<BasicRfcServer>,IProgress<string>?,CancellationToken,Task<BasicDiscovery>>? Discover {get;init;}
}

public sealed class BasicConnectionService(ISecretStore secrets)
{
    internal BasicValidationHooks Hooks {get;init;}=new();
    internal static void ValidateDraft(BasicDraft d)
    {
        BasicPolicy.ParseHost(d.Host);BasicPolicy.ManualUrl(d.ManualUrl,d.Host);
        if(d.Sysnr.Trim().Length!=2||!d.Sysnr.Trim().All(char.IsAsciiDigit))throw new InvalidDataException(UiText.Get("Basic.InstanceRequired"));
        var sid=d.Sid.Trim().ToUpperInvariant();if(sid.Length!=3||!sid.All(char.IsAsciiLetterOrDigit))throw new InvalidDataException(UiText.Get("Sid.Required"));
        var p=new ConnectionProfile{Name=d.User,TargetKey="draft",User=d.User.Trim(),Client=d.Client.Trim(),Language=d.Language.Trim().ToUpperInvariant()};p.Validate();
        if(!p.Language.All(char.IsAsciiLetter))throw new InvalidDataException(UiText.Get("Text029"));
    }
    public async Task<AppSettings> SaveDraftAsync(AppSettings source,SettingsStore store,BasicDraft draft,CancellationToken ct=default)
    {
        ValidateDraft(draft);
        await using var snapshot=await Snapshot.CreateAsync(source,draft,secrets,ct);
        snapshot.Profile.AdtBaseUrl=string.IsNullOrWhiteSpace(draft.ManualUrl)?snapshot.Profile.AdtBaseUrl:BasicPolicy.ManualUrl(draft.ManualUrl,draft.Host);
        snapshot.Profile.BasicValidation=null;
        return await snapshot.CommitAsync(store,null,ct);
    }
    public async Task<BasicValidationResult> ValidateAsync(AppSettings source,SettingsStore store,BasicDraft draft,bool rfcAvailable,
        Func<BasicPrompt,CancellationToken,Task<bool>> confirm,IProgress<string>? progress=null,CancellationToken ct=default)
    {
        ValidateDraft(draft);
        var empty=new BasicDiscovery(2,[],false,false,0);
        ct.ThrowIfCancellationRequested();if(!await confirm(BasicPrompt.ConnectionWarning,ct))throw new OperationCanceledException(ct);
        ct.ThrowIfCancellationRequested();
        await using var snapshot=await Snapshot.CreateAsync(source,draft,secrets,ct);
        var rfc=BasicCheck.Blocked("RFC_DEPENDENCY_MISSING");var servers=new BasicRfcServers("RFC_UNAVAILABLE",[]);
        if(rfcAvailable){
            progress?.Report(UiText.Get("Basic.RfcChecking"));rfc=await CheckAsync(snapshot,"rfc",ct);
            MatchSid(draft,rfc);
            if(rfc.Success){
                progress?.Report(UiText.Get("Basic.RfcAuthorityChecking"));
                servers=Hooks.Servers!=null?await Hooks.Servers(snapshot.Settings,snapshot.Path,snapshot.Entry.TargetKey,ct):await ReadServersAsync(snapshot,ct);
                if(!servers.Available)progress?.Report(UiText.Get(servers.Code=="AUTHORIZATION_DENIED"?"Basic.RfcNoAuthority":servers.Code=="RFC_ALLOWLIST_DENIED"?"Basic.RfcAllowlistDenied":"Basic.RfcDiscoveryUnavailable"));
            }
        }
        var discovery=empty;var mode="smart";
        async Task<BasicDiscovery> Discover(string selected){
            ct.ThrowIfCancellationRequested();
            var result=Hooks.Discover!=null?await Hooks.Discover(source,draft,selected,servers.Servers,progress,ct):await new BasicDiscoveryService().DiscoverAsync(source,draft,selected,servers.Servers,progress,ct);
            ct.ThrowIfCancellationRequested();BasicDiscoveryService.ValidateTargets(draft,result,selected,servers.Servers);return result;
        }
        if(!string.IsNullOrWhiteSpace(draft.ManualUrl)){mode="manual";discovery=await Discover(mode);}
        else {
            if(servers.Available&&await confirm(BasicPrompt.RfcDiscovery,ct)){
                mode="rfc";progress?.Report(UiText.Get("Basic.RfcReading",servers.Servers.Count));discovery=await Discover(mode);
            }
            if(!BasicPolicy.Usable(discovery).Any()){
                ct.ThrowIfCancellationRequested();
                if(await confirm(BasicPrompt.AutomaticScan,ct)){
                    mode="smart";discovery=await Discover(mode);
                    if(!BasicPolicy.Usable(discovery).Any()&&await confirm(BasicPrompt.ExtendedScan,ct)){mode="full";discovery=await Discover(mode);}
                }
            }
        }
        ct.ThrowIfCancellationRequested();
        var (adt,url)=await ValidateCandidatesAsync(snapshot,draft,discovery,mode,servers.Servers,progress,ct);
        MatchSid(draft,adt);
        if(!adt.Success&&!rfc.Success)return new(null,adt,rfc,discovery);
        var next=await snapshot.CommitAsync(store,(adt,rfc),ct);
        return new(next,adt,rfc,discovery,url);
    }
    // Kept for callers that explicitly supply an already completed anonymous discovery.
    public async Task<(AppSettings? Settings,BasicCheck Adt,BasicCheck Rfc)> SaveAsync(AppSettings source,SettingsStore store,BasicDraft draft,BasicDiscovery discovery,bool rfcAvailable,CancellationToken ct=default,
        Func<AppSettings,string,string,CancellationToken,Task<BasicCheck>>? tester=null)
    {
        ValidateDraft(draft);BasicDiscoveryService.ValidateTargets(draft,discovery,"smart",[]);await using var snapshot=await Snapshot.CreateAsync(source,draft,secrets,ct);
        var rfc=rfcAvailable?await CheckAsync(snapshot,"rfc",ct,tester):BasicCheck.Blocked("RFC_DEPENDENCY_MISSING");MatchSid(draft,rfc);
        var (adt,_)=await ValidateCandidatesAsync(snapshot,draft,discovery,"smart",[],null,ct,tester);MatchSid(draft,adt);
        return (!adt.Success&&!rfc.Success?null:await snapshot.CommitAsync(store,(adt,rfc),ct),adt,rfc);
    }
    private static void MatchSid(BasicDraft draft,BasicCheck check)
    {
        if(check.Success&&!string.IsNullOrWhiteSpace(check.Sid)&&!draft.Sid.Trim().Equals(check.Sid.Trim(),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException(UiText.Get("Sid.Mismatch"));
    }
    private async Task<(BasicCheck Check,string Url)> ValidateCandidatesAsync(Snapshot snapshot,BasicDraft draft,BasicDiscovery discovery,string mode,IReadOnlyList<BasicRfcServer> servers,IProgress<string>? progress,CancellationToken ct,
        Func<AppSettings,string,string,CancellationToken,Task<BasicCheck>>? tester=null)
    {
        // Validate the complete set before any candidate receives credentials.
        BasicDiscoveryService.ValidateTargets(draft,discovery,mode,servers);
        var previous=snapshot.Profile.AdtBaseUrl;var result=BasicCheck.Failed(BasicPolicy.BestFailure(discovery));var failures=new List<BasicCandidate>();
        foreach(var candidate in BasicPolicy.Usable(discovery)){
            ct.ThrowIfCancellationRequested();snapshot.Profile.AdtBaseUrl=AdtProfiles.NormalizeUrl(candidate.Url);
            progress?.Report(UiText.Get("Basic.Authenticating",snapshot.Profile.AdtBaseUrl));
            result=await CheckAsync(snapshot,"adt",ct,tester);
            if(result.Success){MatchSid(draft,result);result=result with{Code=new Uri(candidate.Url).Scheme=="http"?"OK_HTTP_PLAINTEXT":"OK_TLS_UNVERIFIED"};break;}
            failures.Add(candidate with{Verified=false,HttpStatus=null,Code=result.Code});
        }
        if(!result.Success){snapshot.Profile.AdtBaseUrl=previous;if(failures.Count>0)result=BasicCheck.Failed(BasicPolicy.BestFailure(discovery with{Candidates=failures}));}
        await new RollingLog(System.IO.Path.Combine(snapshot.Source.DataDirectory,"logs","manager")).WriteAsync(LogLevel.Info,"basic.validation",new{discovery.DiscoveryId,stage="adt",result.State,code=BasicPolicy.SafeCode(result.Code)},ct);
        progress?.Report(result.Success?UiText.Get("Basic.ValidationSuccess",snapshot.Profile.AdtBaseUrl):UiText.Get("Basic.ManualFallback"));
        return (result,result.Success?snapshot.Profile.AdtBaseUrl:"");
    }
    private async Task<BasicCheck> CheckAsync(Snapshot snapshot,string component,CancellationToken ct,Func<AppSettings,string,string,CancellationToken,Task<BasicCheck>>? tester=null)
    {
        await snapshot.MaterializeAsync(ct);
        var check=tester??Hooks.Check;
        if(check!=null)return await check(snapshot.Settings,snapshot.Path,component,ct);
        try {
            var response=await CallAsync(snapshot,component,component=="adt"?"diagnose_connection":"sap_rfc_ping_system",new{target_key=snapshot.Entry.TargetKey},ct);
            if(component=="rfc"){
                var r=BasicPolicy.RfcResult(response);
                if(r.Success&&response.GetProperty("structuredContent").TryGetProperty("attributes",out var attrs)){
                    string? Get(params string[] names){foreach(var name in names)if(attrs.TryGetProperty(name,out var v)&&v.ValueKind==JsonValueKind.String)return v.GetString();return null;}
                    return r with{Sid=Get("sysId","sysid"),Host=Get("partnerHost","partner_host","partnerhost"),Release=Get("partnerRel","partner_rel","partnerrel")};
                }return r;
            }
            if(response.TryGetProperty("structuredContent",out var data))return BasicPolicy.AdtResult(data);
            if(response.TryGetProperty("content",out var content))foreach(var item in content.EnumerateArray())if(item.TryGetProperty("text",out var text))try{using var json=JsonDocument.Parse(text.GetString()!);return BasicPolicy.AdtResult(json.RootElement);}catch(JsonException){}
            return BasicCheck.Failed("ADT_RESPONSE_INVALID");
        }catch(OperationCanceledException)when(!ct.IsCancellationRequested){return BasicCheck.Failed(component=="rfc"?"RFC_TIMEOUT":"NETWORK_TIMEOUT");}
        catch(OperationCanceledException){throw;}
        catch{return BasicCheck.Failed(component=="rfc"?"RFC_PROCESS_FAILED":"ADT_PROCESS_FAILED");}
    }
    private static async Task<JsonElement> CallAsync(Snapshot snapshot,string component,string tool,object arguments,CancellationToken ct)
    {
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(component=="rfc"?10:45));
        await using var session=await McpSession.StartAsync(snapshot.Settings,snapshot.Path,component,timeout.Token);
        return await session.RequestAsync("tools/call",new{name=tool,arguments},timeout.Token);
    }
    private static async Task<BasicRfcServers> ReadServersAsync(Snapshot snapshot,CancellationToken ct)
    {
        try {
            var response=await CallAsync(snapshot,"rfc","sap_rfc_invoke",new{target_key=snapshot.Entry.TargetKey,function_name="TH_SERVER_LIST",parameters=new{}},ct);
            return ParseServers(response);
        }catch(OperationCanceledException)when(!ct.IsCancellationRequested){return new("RFC_TIMEOUT",[]);}
        catch(OperationCanceledException){throw;}
        catch{return new("RFC_DISCOVERY_FAILED",[]);}
    }
    internal static BasicRfcServers ParseServers(JsonElement response)
    {
        if(response.TryGetProperty("isError",out var error)&&error.ValueKind==JsonValueKind.True){
            var raw=response.GetRawText();var code=raw.Contains("allowlist",StringComparison.OrdinalIgnoreCase)?"RFC_ALLOWLIST_DENIED":new[]{"S_RFC","RFC_NO_AUTHORITY","AUTHORIZATION","NOT_AUTHORIZED","NO_AUTHORITY"}.Any(x=>raw.Contains(x,StringComparison.OrdinalIgnoreCase))?"AUTHORIZATION_DENIED":"RFC_DISCOVERY_FAILED";
            return new(code,[]);
        }
        var servers=new List<BasicRfcServer>();
        void Walk(JsonElement value,int depth){
            if(depth>12||servers.Count>=4096)return;
            if(value.ValueKind==JsonValueKind.Array){foreach(var row in value.EnumerateArray())Walk(row,depth+1);}
            if(value.ValueKind!=JsonValueKind.Object)return;
            string Get(string name)=>value.TryGetProperty(name,out var p)&&p.ValueKind==JsonValueKind.String?p.GetString()!.Trim():"";
            var name=Get("NAME");var sysnr=Get("SYSNR");if(sysnr.Length==0){var match=Regex.Match(name,@"_(\d{2})$");if(match.Success)sysnr=match.Groups[1].Value;}
            var host=Get("HOSTADDR_V4_STR");if(host is "" or "0.0.0.0")host=Get("HOST");if(host.Length==0)host=Get("HOSTNAME");
            if(host.Length==0&&name.Length>0){var match=Regex.Match(name,@"^(.+)_[A-Za-z0-9]{3}_\d{2}$");if(match.Success)host=match.Groups[1].Value;}
            if(host.Length>0&&Regex.IsMatch(sysnr,@"^\d{2}$"))try{servers.Add(new(BasicPolicy.ParseHost(host).Host,sysnr));}catch(InvalidDataException){}
            foreach(var property in value.EnumerateObject())if(property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)Walk(property.Value,depth+1);
        }
        if(response.TryGetProperty("structuredContent",out var body))Walk(body,0);
        return new(servers.Count>0?"OK":"RFC_SERVERS_INVALID",servers.Distinct().ToList());
    }

    private sealed class Snapshot:IAsyncDisposable
    {
        public required AppSettings Source {get;init;}
        public required AppSettings Settings {get;init;}
        public required ManagedConnection Entry {get;init;}
        public required ConnectionProfile Profile {get;init;}
        public required string Root {get;init;}
        public string Path=>System.IO.Path.Combine(Root,"settings.json");
        private string? _newSecret;
        public static async Task<Snapshot> CreateAsync(AppSettings source,BasicDraft draft,ISecretStore secrets,CancellationToken ct)
        {
            var host=BasicPolicy.ParseHost(draft.Host).Host;var entry=new ManagedConnection{Id=draft.SystemId,Name=host,Host=host,Sysnr=draft.Sysnr.Trim(),Sid=draft.Sid.Trim().ToUpperInvariant()};entry.NormalizeAndValidate();
            var catalog=await new ManagedJsonConnectionSource().ReadCatalogAsync(source,ct,createIfMissing:true);
            var existing=catalog.Systems.SingleOrDefault(s=>s.Id==entry.Id)??catalog.Systems.SingleOrDefault(s=>s.TargetKey.Equals(entry.TargetKey,StringComparison.OrdinalIgnoreCase));
            if(existing!=null){entry.Id=existing.Id;entry.Name=existing.Name;entry.Router=existing.Router;}
            var original=source.Profiles.SingleOrDefault(p=>p.Id==draft.ProfileId);var profile=original==null?new ConnectionProfile{Id=draft.ProfileId,Name=draft.User}:BasicPolicy.Copy(original);
            if(original!=null)profile.SecretRef=System.IO.Path.GetFullPath(ProfileService.ResolveSecret(source.CredentialsFile,profile.SecretRef));
            if(original==null||!original.TargetKey.Equals(entry.TargetKey,StringComparison.OrdinalIgnoreCase))profile.AdtBaseUrl="";
            profile.TargetKey=entry.TargetKey;profile.Sid=entry.Sid;profile.User=draft.User.Trim();profile.Client=draft.Client.Trim();profile.Language=draft.Language.Trim().ToUpperInvariant();profile.AllowInsecureTls=true;
            // Stored Advanced certificates are retained, but are not needed by Basic's transport.
            profile.Validate();
            var root=System.IO.Path.Combine(source.DataDirectory,"cache","basic-tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
            var settings=BasicPolicy.Copy(source);settings.Profiles=[profile];settings.ActiveProfiles=new(){{entry.TargetKey,profile.Id}};settings.ConnectionSourceMode="managed";
            settings.LandscapeFile=System.IO.Path.Combine(root,"landscape.xml");settings.CredentialsFile=System.IO.Path.Combine(root,"mapping.json");
            settings.Adt.Selected=true;settings.Rfc.Selected=true;settings.Adt.LogLevel=settings.Rfc.LogLevel=LogLevel.Off;
            settings.Adt.LogDirectory=System.IO.Path.Combine(root,"adt");settings.Rfc.LogDirectory=System.IO.Path.Combine(root,"rfc");
            var snapshot=new Snapshot{Source=source,Settings=settings,Entry=entry,Profile=profile,Root=root};
            try {
                if(draft.Password.Length>0){snapshot._newSecret=await secrets.ProtectAsync(draft.Password,System.IO.Path.Combine(root,"secrets"),ct);profile.SecretRef=snapshot._newSecret;}
                if(string.IsNullOrWhiteSpace(profile.SecretRef))throw new InvalidDataException(UiText.Get("Basic.PasswordRequired"));
                await secrets.ReadAsync(profile.SecretRef,ct);
                await AtomicFile.WriteAsync(settings.LandscapeFile,ManagedJsonConnectionSource.ExportXml(new(){Systems=[entry]}),ct,true);
                return snapshot;
            }catch{await snapshot.DisposeAsync();throw;}
        }
        public async Task MaterializeAsync(CancellationToken ct){await ProfileService.MaterializeAsync(Settings,ct);await new SettingsStore(Path).SaveAsync(Settings,ct);}
        public async Task<AppSettings> CommitAsync(SettingsStore store,(BasicCheck Adt,BasicCheck Rfc)? validation,CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();string? committed=null;
            try {
                if(_newSecret!=null){Directory.CreateDirectory(Source.SecretsDirectory);committed=System.IO.Path.Combine(Source.SecretsDirectory,System.IO.Path.GetFileName(_newSecret));await AtomicFile.WriteAsync(committed,await File.ReadAllTextAsync(_newSecret,ct),ct,true);Profile.SecretRef=committed;}
                Profile.SecretRef=System.IO.Path.GetRelativePath(System.IO.Path.GetDirectoryName(Source.CredentialsFile)!,Profile.SecretRef).Replace('\\','/');
                Profile.BasicValidation=validation is {} v?new(v.Adt,v.Rfc,DateTimeOffset.UtcNow,BasicPolicy.Fingerprint(Source,Profile,Entry.Sid),2):null;
                return await new ConnectionManager(store).SaveBasicAsync(Source,Entry,Profile,ct);
            }catch{if(committed!=null)File.Delete(committed);throw;}
        }
        public ValueTask DisposeAsync(){if(Directory.Exists(Root))Directory.Delete(Root,true);return ValueTask.CompletedTask;}
    }
}
