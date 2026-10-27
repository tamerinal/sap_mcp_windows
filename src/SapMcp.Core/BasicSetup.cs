using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace SapMcp.Core;

public sealed record BasicCheck(string State, string Code)
{
    public bool Success => State=="passed";
    public static BasicCheck Passed => new("passed","OK");
    public static BasicCheck Failed(string code) => new("failed",code);
    public static BasicCheck Blocked(string code) => new("blocked",code);
}
public sealed record BasicValidation(BasicCheck Adt, BasicCheck Rfc, DateTimeOffset TestedAt, string Fingerprint);
public sealed record BasicDiscoveryOptions(int Concurrency=16,int ProbeTimeoutMs=1500,int BudgetMs=15000,int DebounceMs=500,int MaxAgeSeconds=60);
public sealed record BasicCandidate(string Url,int Priority,string Code,int? HttpStatus,bool Verified);
public sealed record BasicDiscovery(int SchemaVersion,List<BasicCandidate> Candidates,bool BudgetExpired,bool Found,int DurationMs);
public sealed class BasicDraft
{
    public string ProfileId {get;set;}=Guid.NewGuid().ToString("N");
    public string SystemId {get;set;}=Guid.NewGuid().ToString("N");
    public string Host {get;set;}="";
    public string Sysnr {get;set;}="";
    public string User {get;set;}="";
    public string Password {get;set;}="";
    public string Client {get;set;}="100";
    public string Language {get;set;}="EN";
    public string ManualUrl {get;set;}="";
    public List<TrustedCertificate> Certificates {get;set;}=[];
}

public static class BasicPolicy
{
    public static readonly BasicDiscoveryOptions Discovery=new();
    public static T Copy<T>(T value)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value,JsonDefaults.Options),JsonDefaults.Options)!;
    public static (string Host,int? Port) ParseHost(string input)
    {
        if(!Uri.TryCreate("https://"+input.Trim(),UriKind.Absolute,out var u)||u.UserInfo.Length>0||u.AbsolutePath!="/"||u.Query.Length>0||u.Fragment.Length>0||input.Contains('\\')||Uri.CheckHostName(u.Host.Trim('[',']'))==UriHostNameType.Unknown)
            throw new InvalidDataException(UiText.Get("Basic.InvalidHost"));
        return (u.Host.Trim('[',']').ToLowerInvariant(),u.IsDefaultPort?null:u.Port);
    }
    public static string ManualUrl(string value,string host)
    {
        if(string.IsNullOrWhiteSpace(value))return "";
        if(value.All(char.IsAsciiDigit))
        {
            if(!int.TryParse(value,out var port)||port is <1 or >65535)throw new InvalidDataException(UiText.Get("Basic.HttpsOnly"));
            var h=ParseHost(host).Host;return $"https://{(h.Contains(':')?"["+h+"]":h)}:{port}";
        }
        var url=AdtProfiles.NormalizeUrl(value);if(!url.StartsWith("https://",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException(UiText.Get("Basic.HttpsOnly"));return url;
    }
    public static int? InstanceCandidate(string url,string host)
    {
        if(!Uri.TryCreate(url,UriKind.Absolute,out var u)||!u.Host.Trim('[',']').Equals(host,StringComparison.OrdinalIgnoreCase))return null;
        return u.Port is >=44300 and <=44399?u.Port-44300:null;
    }
    public static string Fingerprint(AppSettings s,ConnectionProfile p)
    {
        var secret=ProfileService.ResolveSecret(s.CredentialsFile,p.SecretRef);
        var cipher=File.Exists(secret)?Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(secret))):"missing";
        var data=new {p.TargetKey,p.User,p.Client,p.Language,p.AdtBaseUrl,p.AdtCertificates,p.AllowInsecureTls,cipher,s.SdkDirectory,s.NodeExecutable,s.PythonExecutable,adt=s.Adt.Version,rfc=s.Rfc.Version};
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data,JsonDefaults.Options))));
    }
    public static string[] Eligible(AppSettings s,ConnectionProfile p)=>s.ActiveProfiles.GetValueOrDefault(p.TargetKey)==p.Id&&p.BasicValidation is {} v&&v.Fingerprint==Fingerprint(s,p)
        ? new[]{v.Adt.Success?"adt":"",v.Rfc.Success?"rfc":""}.Where(x=>x.Length>0).ToArray():[];
    public static BasicCheck AdtResult(JsonElement report)
    {
        if(report.TryGetProperty("success",out var success)&&success.ValueKind==JsonValueKind.True)return BasicCheck.Passed;
        if(report.TryGetProperty("stages",out var stages))foreach(var row in stages.EnumerateArray())
            if(row.GetProperty("status").GetString() is "failed" or "unverified")return BasicCheck.Failed(SafeCode(row.GetProperty("code").GetString()));
        return BasicCheck.Failed("ADT_NOT_VERIFIED");
    }
    public static string SafeCode(string? code)=>code!=null&&code.Length<=70&&code.All(c=>char.IsAsciiLetterUpper(c)||c=='_'||char.IsAsciiDigit(c))?code:"CONNECTION_FAILED";
    public static BasicCheck RfcResult(JsonElement result)
    {
        if(result.TryGetProperty("isError",out var e)&&e.ValueKind==JsonValueKind.True)
        {
            // Upstream RFC errors are not displayed/logged; inspect only to stop credential retries.
            var raw=result.GetRawText();
            return BasicCheck.Failed(raw.Contains("LOGON",StringComparison.OrdinalIgnoreCase)||raw.Contains("Authentication",StringComparison.OrdinalIgnoreCase)?"AUTHENTICATION_FAILED":"RFC_PING_FAILED");
        }
        return result.TryGetProperty("structuredContent",out var content)&&content.TryGetProperty("attributes",out var attributes)&&attributes.ValueKind==JsonValueKind.Object?BasicCheck.Passed:BasicCheck.Failed("RFC_RESPONSE_INVALID");
    }
}

public sealed class BasicDiscoveryService
{
    public async Task<BasicDiscovery> DiscoverAsync(AppSettings settings,BasicDraft draft,CancellationToken ct=default)
    {
        var (host,port)=BasicPolicy.ParseHost(draft.Host);
        var manual=BasicPolicy.ManualUrl(draft.ManualUrl,draft.Host);var url=manual.Length==0?null:manual;
        if(url!=null&&!url.StartsWith("https://",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException(UiText.Get("Basic.HttpsOnly"));
        var entry=Path.Combine(settings.PackageDirectory,"adt","dist","basic-connection.js");
        if(!File.Exists(entry)||!File.Exists(settings.NodeExecutable))throw new InvalidOperationException(UiText.Get("Basic.PrepareFirst"));
        var input=new {host,explicitPort=port,manualUrl=url,certificates=draft.Certificates,options=new {concurrency=BasicPolicy.Discovery.Concurrency,probeTimeoutMs=BasicPolicy.Discovery.ProbeTimeoutMs,budgetMs=BasicPolicy.Discovery.BudgetMs}};
        var run=await ProcessRunner.RunAsync(settings.NodeExecutable,[entry],timeout:TimeSpan.FromSeconds(20),stdin:JsonSerializer.Serialize(input,JsonDefaults.Options),ct:ct);
        try
        {
            var result=JsonSerializer.Deserialize<BasicDiscovery>(run.Output,JsonDefaults.Options);
            if(run.ExitCode!=0||result is not {SchemaVersion:1}||result.Candidates==null||result.Candidates.Count>102)throw new InvalidDataException();
            foreach(var candidate in result.Candidates)
            {
                var normalized=AdtProfiles.NormalizeUrl(candidate.Url);var parsed=new Uri(normalized);
                if(parsed.Scheme!="https"||(url!=null?normalized!=url:!parsed.Host.Trim('[',']').Equals(host,StringComparison.OrdinalIgnoreCase)))throw new InvalidDataException();
            }
            return result;
        }
        catch(Exception ex)when(ex is JsonException or InvalidDataException){throw new InvalidDataException(UiText.Get("Basic.DiscoveryInvalid"));}
    }
}

public sealed class BasicPreparationService(PackageService packages)
{
    public IReadOnlyList<Requirement> LastChecks {get;private set;}=[];
    public async Task<AppSettings> PrepareAsync(AppSettings current,string sdk,bool adtOnly,IProgress<string>? progress,CancellationToken ct)
    {
        var s=BasicPolicy.Copy(current);s.Adt.Selected=true;s.Rfc.Selected=!adtOnly;s.SdkDirectory=adtOnly?current.SdkDirectory:sdk;
        if(!adtOnly&&string.IsNullOrWhiteSpace(sdk))throw new InvalidDataException(UiText.Get("Basic.SdkRequired"));
        if(!adtOnly&&!Path.IsPathFullyQualified(sdk))throw new InvalidDataException(UiText.Get("Basic.SdkRequired"));
        var prerequisites=new PrerequisiteService(packages);var checks=await prerequisites.CheckAsync(s,ct);
        var fatal=checks.FirstOrDefault(c=>c.State is CheckState.Blocked or CheckState.Missing&&!c.Installable&&c.Id!="sdk");
        if(fatal!=null)throw new InvalidOperationException(UiText.Get("Basic.RequirementBlocked",fatal.Name));
        await prerequisites.InstallAsync(s,checks.Where(c=>c.Id=="node"&&c.Installable&&c.State!=CheckState.Ready).Select(c=>c.Id),progress,ct);
        foreach(var dependency in checks.Where(c=>c.Id!="node"&&c.Installable&&c.State!=CheckState.Ready))
        {
            try{await prerequisites.InstallAsync(s,[dependency.Id],progress,ct);}
            catch(OperationCanceledException){throw;}
            catch{progress?.Report(UiText.Get("Basic.RequirementBlocked",dependency.Name));}
        }
        checks=await prerequisites.CheckAsync(s,ct);
        LastChecks=checks;
        var sharedFailure=checks.FirstOrDefault(c=>c.Id is not ("sdk" or "python" or "pyrfc" or "vc-runtime" or "vc2013")&&c.State is not (CheckState.Ready or CheckState.Warning));
        if(sharedFailure!=null)throw new InvalidOperationException(UiText.Get("Basic.RequirementBlocked",sharedFailure.Name));
        var rfcReady=!adtOnly&&checks.Where(c=>c.Id is "sdk" or "python" or "pyrfc" or "vc-runtime" or "vc2013").All(c=>c.State==CheckState.Ready);
        var catalog=packages.LoadCatalog();
        foreach(var component in new[]{"adt","rfc"}.Where(c=>c=="adt"||rfcReady))
        {
            var asset=catalog.Find(component);var root=Path.Combine(s.PackageDirectory,component);
            if(s.Component(component).Version!=asset.Version||!File.Exists(Path.Combine(root,"dist","server.js"))||(component=="adt"&&!File.Exists(Path.Combine(root,"dist","basic-connection.js"))))
                await packages.InstallAssetAsync(asset,root,Path.Combine(s.DataDirectory,"cache"),progress,ct);
            s.Component(component).Installed=true;s.Component(component).Version=asset.Version;
        }
        // Preserve Advanced's component selection; readiness for Basic is reported independently.
        s.Rfc.Selected=current.Rfc.Selected;s.Adt.Selected=current.Adt.Selected;
        return s;
    }
}

public sealed class BasicConnectionService(ISecretStore secrets)
{
    public async Task<(AppSettings? Settings,BasicCheck Adt,BasicCheck Rfc)> SaveAsync(AppSettings source,SettingsStore store,BasicDraft draft,BasicDiscovery discovery,bool rfcAvailable,CancellationToken ct=default,
        Func<AppSettings,string,string,CancellationToken,Task<BasicCheck>>? tester=null)
    {
        var (host,_)=BasicPolicy.ParseHost(draft.Host);
        var sysnr=draft.Sysnr.Trim();if(sysnr.Length!=2||!sysnr.All(char.IsAsciiDigit))throw new InvalidDataException(UiText.Get("Basic.InstanceRequired"));
        var entry=new ManagedConnection{Id=draft.SystemId,Name=host,Host=host,Sysnr=sysnr};entry.NormalizeAndValidate();
        var catalog=await new ManagedJsonConnectionSource().ReadCatalogAsync(source,ct,createIfMissing:true);
        var existingSystem=catalog.Systems.SingleOrDefault(s=>s.Id==entry.Id)??catalog.Systems.SingleOrDefault(s=>s.TargetKey.Equals(entry.TargetKey,StringComparison.OrdinalIgnoreCase));
        if(existingSystem!=null){entry.Id=existingSystem.Id;entry.Name=existingSystem.Name;entry.Router=existingSystem.Router;entry.Sid=existingSystem.Sid;}
        var original=source.Profiles.SingleOrDefault(p=>p.Id==draft.ProfileId);
        var profile=original==null?new ConnectionProfile{Id=draft.ProfileId,Name=draft.User}:BasicPolicy.Copy(original);
        profile.TargetKey=entry.TargetKey;profile.User=draft.User.Trim();profile.Client=draft.Client.Trim();profile.Language=draft.Language.Trim().ToUpperInvariant();profile.AllowInsecureTls=false;profile.AdtCertificates=draft.Certificates.ToList();
        profile.Name=string.IsNullOrWhiteSpace(profile.Name)?profile.User:profile.Name;profile.AdtBaseUrl="";
        if(original!=null)profile.SecretRef=Path.GetFullPath(ProfileService.ResolveSecret(source.CredentialsFile,original.SecretRef));
        profile.Validate();if(!profile.Language.All(char.IsAsciiLetter))throw new InvalidDataException(UiText.Get("Text029"));
        var root=Path.Combine(source.DataDirectory,"cache","basic-tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        string? newSecret=null;
        try
        {
            if(draft.Password.Length>0){newSecret=await secrets.ProtectAsync(draft.Password,Path.Combine(root,"secrets"),ct);profile.SecretRef=newSecret;}
            if(string.IsNullOrWhiteSpace(profile.SecretRef))throw new InvalidDataException(UiText.Get("Basic.PasswordRequired"));
            await secrets.ReadAsync(profile.SecretRef,ct);
            var test=BasicPolicy.Copy(source);test.Profiles=[profile];test.ActiveProfiles=new(){{entry.TargetKey,profile.Id}};
            test.LandscapeFile=Path.Combine(root,"landscape.xml");test.CredentialsFile=Path.Combine(root,"mapping.json");test.ConnectionSourceMode="managed";
            test.Rfc.Selected=rfcAvailable;test.Adt.Selected=true;test.Rfc.LogDirectory=Path.Combine(root,"rfc");test.Adt.LogDirectory=Path.Combine(root,"adt");test.Rfc.LogLevel=LogLevel.Off;test.Adt.LogLevel=LogLevel.Off;
            await AtomicFile.WriteAsync(test.LandscapeFile,ManagedJsonConnectionSource.ExportXml(new(){Systems=[entry]}),ct,privateData:true);
            var testPath=Path.Combine(root,"settings.json");
            async Task<BasicCheck> Run(string component)
            {
                await ProfileService.MaterializeAsync(test,ct);await new SettingsStore(testPath).SaveAsync(test,ct);
                try
                {
                    if(tester!=null)return await tester(test,testPath,component,ct);
                    await using var session=await McpSession.StartAsync(test,testPath,component,ct);
                    var response=await session.RequestAsync("tools/call",new{name=component=="adt"?"diagnose_connection":"sap_rfc_ping_system",arguments=new{target_key=entry.TargetKey}},ct);
                    if(component=="rfc")return BasicPolicy.RfcResult(response);
                    if(response.TryGetProperty("structuredContent",out var data))return BasicPolicy.AdtResult(data);
                    if(response.TryGetProperty("content",out var content))foreach(var item in content.EnumerateArray())if(item.TryGetProperty("text",out var text))
                        try{using var json=JsonDocument.Parse(text.GetString()!);return BasicPolicy.AdtResult(json.RootElement);}catch(JsonException){}
                    return BasicCheck.Failed("ADT_RESPONSE_INVALID");
                }
                catch(OperationCanceledException){throw;}
                catch{return BasicCheck.Failed(component=="adt"?"ADT_PROCESS_FAILED":"RFC_PROCESS_FAILED");}
            }
            var adt=BasicCheck.Failed(discovery.Candidates.Select(c=>c.Code).FirstOrDefault(c=>c.StartsWith("TLS_",StringComparison.Ordinal)||c is "AUTHORIZATION_DENIED" or "DNS_FAILED" or "NETWORK_TIMEOUT" or "ADT_REDIRECT_REQUIRES_URL" or "ADT_DISCOVERY_INVALID" or "ADT_UNEXPECTED_CONTENT")??"ADT_NOT_FOUND");
            foreach(var c in discovery.Candidates.Where(c=>c.Verified||c.HttpStatus==401).OrderBy(c=>c.Verified?0:1).ThenBy(c=>c.Priority))
            {
                var normalized=AdtProfiles.NormalizeUrl(c.Url);var u=new Uri(normalized);
                var allowed=draft.ManualUrl.Length>0?normalized==BasicPolicy.ManualUrl(draft.ManualUrl,draft.Host):u.Host.Trim('[',']').Equals(host,StringComparison.OrdinalIgnoreCase);
                if(u.Scheme!="https"||!allowed)throw new InvalidDataException(UiText.Get("Basic.DiscoveryInvalid"));
                profile.AdtBaseUrl=normalized;adt=await Run("adt");if(adt.Success||adt.Code=="AUTHENTICATION_FAILED")break;
            }
            var rfc=adt.Code=="AUTHENTICATION_FAILED"?BasicCheck.Blocked("AUTHENTICATION_RETRY_STOPPED"):rfcAvailable?await Run("rfc"):BasicCheck.Blocked("RFC_DEPENDENCY_MISSING");
            if(!adt.Success&&!rfc.Success)return(null,adt,rfc);
            // Copy the newly protected secret only when committing a usable profile.
            string? committedSecret=null;
            try
            {
                if(newSecret!=null){Directory.CreateDirectory(source.SecretsDirectory);committedSecret=Path.Combine(source.SecretsDirectory,Path.GetFileName(newSecret));await AtomicFile.WriteAsync(committedSecret,await File.ReadAllTextAsync(newSecret,ct),ct,privateData:true);profile.SecretRef=committedSecret;}
                profile.SecretRef=Path.GetRelativePath(Path.GetDirectoryName(source.CredentialsFile)!,profile.SecretRef).Replace('\\','/');
                profile.BasicValidation=new(adt,rfc,DateTimeOffset.UtcNow,BasicPolicy.Fingerprint(source,profile));
                var next=await new ConnectionManager(store).SaveBasicAsync(source,entry,profile,ct);return(next,adt,rfc);
            }
            catch{if(committedSecret!=null)File.Delete(committedSecret);throw;}
        }
        finally {if(Directory.Exists(root))Directory.Delete(root,true);}
    }
}
