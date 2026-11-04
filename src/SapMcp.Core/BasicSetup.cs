using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SapMcp.Core;

public sealed record BasicCheck(string State, string Code, string? Sid = null, string? Host = null, string? Release = null)
{
    public bool Success => State=="passed";
    public static BasicCheck Passed => new("passed","OK");
    public static BasicCheck Failed(string code) => new("failed",code);
    public static BasicCheck Blocked(string code) => new("blocked",code);
}
public sealed record BasicValidation(BasicCheck Adt, BasicCheck Rfc, DateTimeOffset TestedAt, string Fingerprint, int PolicyVersion = 0);
public sealed record BasicDiscoveryOptions(int Concurrency=32,int ProbeTimeoutMs=1500,int BudgetMs=30000,int DebounceMs=500,int MaxAgeSeconds=60,int TcpTimeoutMs=300,int TcpBudgetMs=60000);
public sealed record BasicCandidate(string Url,int Priority,string Code,int? HttpStatus,bool Verified,int DurationMs=0);
public sealed record BasicDiscovery(int SchemaVersion,List<BasicCandidate> Candidates,bool BudgetExpired,bool Found,int DurationMs,string DiscoveryId="",string StartedAt="");
public sealed class BasicDiscoveryException(string code):IOException(UiText.Get("Basic.DiscoveryError",UiText.Get(code switch
{
    "DISCOVERY_PORT_INVALID"=>"Basic.PortInvalid",
    "DISCOVERY_HOST_INVALID"=>"Basic.InvalidHost",
    "BASIC_HTTPS_REQUIRED"=>"Basic.HttpsOnly",
    "DISCOVERY_OPTIONS_INVALID" or "DISCOVERY_INPUT_INVALID"=>"Basic.DiscoveryInputInvalid",
    "DISCOVERY_PROCESS_TIMEOUT"=>"Basic.DiscoveryProcessTimeout",
    "DISCOVERY_PACKAGE_MISSING" or "BASIC_DISCOVERY_UPGRADE_REQUIRED"=>"Basic.PrepareFirst",
    _=>"Basic.DiscoveryInvalid"
}),code))
{public string Code {get;}=code;}
public sealed class BasicDraft
{
    public string ProfileId {get;set;}=Guid.NewGuid().ToString("N");
    public string SystemId {get;set;}=Guid.NewGuid().ToString("N");
    public string Sid {get;set;}="";
    public string Host {get;set;}="";
    public string Sysnr {get;set;}="";
    public string User {get;set;}="";
    public string Password {get;set;}="";
    public string Client {get;set;}="100";
    public string Language {get;set;}="EN";
    public string ManualUrl {get;set;}="";
    public string PreferredUrl {get;set;}="";
    public string Router {get;set;}="";
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
        return AdtProfiles.NormalizeUrl(value);
    }
    public static int? InstanceCandidate(string url,string host)
    {
        if(!Uri.TryCreate(url,UriKind.Absolute,out var u)||!u.Host.Trim('[',']').Equals(host,StringComparison.OrdinalIgnoreCase))return null;
        return u.Port is >=44300 and <=44399?u.Port-44300:null;
    }
    public static string Fingerprint(AppSettings s,ConnectionProfile p,string? sid=null)
    {
        var secret=ProfileService.ResolveSecret(s.CredentialsFile,p.SecretRef);
        var cipher=File.Exists(secret)?Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(secret))):"missing";
        sid ??= File.Exists(s.LandscapeFile)?LandscapeService.Load(s.LandscapeFile).FirstOrDefault(x=>x.TargetKey==p.TargetKey)?.Sid:null;
        var data=new {policyVersion=2,sid,profileSid=p.Sid,p.Extra,p.TargetKey,p.User,p.Client,p.Language,p.AdtBaseUrl,p.AdtCertificates,p.AllowInsecureTls,cipher,s.SdkDirectory,s.NodeExecutable,s.PythonExecutable,adt=s.Adt.Version,rfc=s.Rfc.Version};
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data,JsonDefaults.Options))));
    }
    public static string[] Eligible(AppSettings s,ConnectionProfile p)=>s.ActiveProfiles.GetValueOrDefault(p.TargetKey)==p.Id&&p.BasicValidation is {PolicyVersion:2} v&&v.Fingerprint==Fingerprint(s,p)
        ? new[]{v.Adt.Success?"adt":"",v.Rfc.Success?"rfc":""}.Where(x=>x.Length>0).ToArray():[];
    public static BasicCheck AdtResult(JsonElement report)
    {
        if(!report.TryGetProperty("stages",out var stages)||stages.ValueKind!=JsonValueKind.Array)return BasicCheck.Failed("ADT_RESPONSE_INVALID");
        foreach(var row in stages.EnumerateArray())
            if(row.TryGetProperty("status",out var state)&&state.GetString() is "failed" or "unverified")
                return BasicCheck.Failed(SafeCode(row.TryGetProperty("code",out var code)?code.GetString():null));
        foreach(var required in new[]{"authentication","discovery","readOnly"})
            if(!stages.EnumerateArray().Any(row=>row.TryGetProperty("stage",out var name)&&name.GetString()==required&&row.TryGetProperty("status",out var status)&&status.GetString()=="passed"))return BasicCheck.Failed("ADT_NOT_VERIFIED");
        return new("passed","OK",report.TryGetProperty("systemId",out var sid)?sid.GetString():null);
    }
    public static int Rank(BasicCandidate c)=>c.Verified?0:c.HttpStatus==401?1:c.Code.StartsWith("TLS_",StringComparison.Ordinal)?2:c.Code.StartsWith("AUTH",StringComparison.Ordinal)?3:c.Code.StartsWith("DNS",StringComparison.Ordinal)||c.Code.StartsWith("TCP",StringComparison.Ordinal)?4:c.Code=="NETWORK_TIMEOUT"?5:6;
    public static string BestFailure(BasicDiscovery result)=>result.Candidates.OrderBy(Rank).ThenBy(c=>c.Priority).Select(c=>SafeCode(c.Code)).FirstOrDefault()??"ADT_NOT_FOUND";
    public static IEnumerable<BasicCandidate> Usable(BasicDiscovery result)=>result.Candidates.Where(c=>c.Verified||c.HttpStatus is 200 or 401)
        .OrderBy(c=>c.Verified?0:c.HttpStatus==200?1:2).ThenBy(c=>new Uri(c.Url).Scheme=="https"?0:1).ThenBy(c=>c.Priority);
    public static string DiscoveryText(BasicDiscovery result)
    {
        var selected=Usable(result).FirstOrDefault();
        var tls=result.Candidates.FirstOrDefault(c=>c.Code.StartsWith("TLS_",StringComparison.Ordinal));
        var text=selected==null?UiText.Get(tls!=null?"Basic.AdvancedTls":"Basic.NotFound"):
            UiText.Get(selected.Verified?"Basic.Discovered":"Basic.AuthCandidate")+" Â· "+selected.Url;
        if(selected==null&&tls==null&&result.Candidates.OrderBy(Rank).FirstOrDefault() is {} failure)text+=" Â· "+SafeCode(failure.Code);
        if(selected!=null&&new Uri(selected.Url).Scheme=="http")text+="\n"+UiText.Get("Basic.HttpWarning");
        if(tls!=null)text+="\n"+UiText.Get("Basic.TlsCandidate")+" Â· "+SafeCode(tls.Code);
        if(result.BudgetExpired)text+="\n"+UiText.Get("Basic.BudgetExpired");
        return text;
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

public static class BasicInstallation
{
    public static bool HasHistory(AppSettings s) => s.BasicSetup != null || s.Adt.Installed || s.Rfc.Installed || s.Profiles.Count > 0 || s.Registrations.Count > 0
        || File.Exists(Path.Combine(s.PackageDirectory, "adt", ".sapmcp-package.json")) || File.Exists(Path.Combine(s.PackageDirectory, "rfc", ".sapmcp-package.json"));
    public static bool IsMaintenance(AppSettings s) => s.BasicSetup is {} state ? state.PreparedAt != null : HasHistory(s);
    public static bool AdtOnly(AppSettings s) => s.BasicSetup?.AdtOnly ?? (HasHistory(s) && !s.Rfc.Installed && string.IsNullOrWhiteSpace(s.SdkDirectory));
    public static string InstalledVersion(AppSettings s, string component)
    {
        var root = Path.Combine(s.PackageDirectory, component);
        var installed = s.Component(component).Version;
        var receiptPath = Path.Combine(root, ".sapmcp-package.json");
        if (File.Exists(receiptPath))
        {
            try { installed = JsonSerializer.Deserialize<PackageAsset>(File.ReadAllText(receiptPath), JsonDefaults.Options)?.Version ?? installed; }
            catch (Exception ex) when (ex is IOException or JsonException) { return ""; }
        }
        return installed;
    }
    public static string Package(AppSettings s, string component, string catalogVersion)
    {
        var root = Path.Combine(s.PackageDirectory, component);
        if (!Directory.Exists(root)) return "Missing";
        var installed = InstalledVersion(s,component);
        if (File.Exists(Path.Combine(root,".sapmcp-package.json")) && installed.Length==0) return "Repair";
        var different = installed != catalogVersion;
        if (different && Version.TryParse(installed.Split('+')[0].Split('-')[0], out var actual) && Version.TryParse(catalogVersion.Split('+')[0].Split('-')[0], out var desired) && actual > desired) different = false;
        if (different) return "Update";
        if (!File.Exists(Path.Combine(root, "dist", "server.js")) || (component == "adt" && !File.Exists(Path.Combine(root, "dist", "basic-connection.js")))) return "Repair";
        return "Current";
    }
    public static bool SharedReady(IEnumerable<Requirement> checks) => checks.Where(c => c.Id is not ("sdk" or "python" or "pyrfc" or "vc-runtime" or "vc2013" or "rfc-native" or "rfc-package")).All(c => c.State is CheckState.Ready or CheckState.Warning);
    public static bool RfcReady(IEnumerable<Requirement> checks) => SharedReady(checks) && new[]{"sdk","python","pyrfc","vc-runtime","vc2013","rfc-native"}.All(id => checks.Any(c => c.Id == id && c.State == CheckState.Ready));
}

public sealed class BasicPreparationService(PackageService packages)
{
    internal Func<AppSettings,CancellationToken,Task<List<Requirement>>>? CheckOverride { get; init; }
    internal Func<AppSettings,IEnumerable<string>,IProgress<string>?,CancellationToken,Task>? InstallOverride { get; init; }
    internal Func<PackageAsset,string,CancellationToken,Task>? InstallPackageOverride { get; init; }
    public IReadOnlyList<Requirement> LastChecks {get;private set;}=[];
    public bool RfcReady {get;private set;}
    internal static void RetainValidatedSdk(AppSettings candidate,AppSettings current,bool ready)
    {if(!ready)candidate.SdkDirectory=current.SdkDirectory;}
    public async Task<AppSettings> PrepareAsync(AppSettings current,string sdk,bool adtOnly,IProgress<string>? progress,CancellationToken ct,bool maintenanceOnly=false)
    {
        var s=BasicPolicy.Copy(current);s.Adt.Selected=true;s.Rfc.Selected=!adtOnly;s.SdkDirectory=adtOnly?current.SdkDirectory:sdk;
        if(!adtOnly&&string.IsNullOrWhiteSpace(sdk))throw new InvalidDataException(UiText.Get("Basic.SdkRequired"));
        if(!adtOnly&&!Path.IsPathFullyQualified(sdk))throw new InvalidDataException(UiText.Get("Basic.SdkRequired"));
        var prerequisites=new PrerequisiteService(packages);
        Task<List<Requirement>> Check() => CheckOverride?.Invoke(s,ct) ?? prerequisites.CheckAsync(s,ct);
        Task Install(IEnumerable<string> ids) => InstallOverride?.Invoke(s,ids,progress,ct) ?? prerequisites.InstallAsync(s,ids,progress,ct);
        var checks=await Check();
        var fatal=checks.FirstOrDefault(c=>c.State is CheckState.Blocked or CheckState.Missing&&!c.Installable&&c.Id is not ("sdk" or "rfc-native"));
        if(fatal!=null)throw new InvalidOperationException(UiText.Get("Basic.RequirementBlocked",fatal.Name));
        var maintenance = maintenanceOnly || BasicInstallation.IsMaintenance(current);
        if (!maintenance && checks.Any(c=>c.Id=="node"&&c.Installable&&c.State!=CheckState.Ready))
            await Install(["node"]);
        foreach(var dependency in checks.Where(c=>!maintenance&&c.Id!="node"&&c.Installable&&c.State!=CheckState.Ready))
        {
            try{await Install([dependency.Id]);}
            catch(OperationCanceledException){throw;}
            catch{progress?.Report(UiText.Get("Basic.RequirementBlocked",dependency.Name));}
        }
        checks=await Check();
        LastChecks=checks;
        var sharedFailure=checks.FirstOrDefault(c=>c.Id is not ("sdk" or "python" or "pyrfc" or "vc-runtime" or "vc2013" or "rfc-native")&&c.State is not (CheckState.Ready or CheckState.Warning));
        if(sharedFailure!=null)throw new InvalidOperationException(UiText.Get(maintenance?"Basic.RepairRequirement":"Basic.RequirementBlocked",sharedFailure.Name));
        var rfcReady=!adtOnly&&BasicInstallation.RfcReady(checks);
        RfcReady=rfcReady;RetainValidatedSdk(s,current,rfcReady);
        var catalog=packages.LoadCatalog();
        foreach(var component in new[]{"adt","rfc"}.Where(c=>c=="adt"||rfcReady))
        {
            var asset=catalog.Find(component);var root=Path.Combine(s.PackageDirectory,component);
            var status = BasicInstallation.Package(s, component, asset.Version);
            if (status == "Repair") throw new InvalidOperationException(UiText.Get("Basic.RepairPackage", component.ToUpperInvariant()));
            if (status is "Missing" or "Update")
            {
                if(InstallPackageOverride!=null)await InstallPackageOverride(asset,root,ct);
                else await packages.InstallAssetAsync(asset,root,Path.Combine(s.DataDirectory,"cache"),progress,ct);
                s.Component(component).Version=asset.Version;
            }
            else s.Component(component).Version=BasicInstallation.InstalledVersion(s,component);
            s.Component(component).Installed=true;
        }
        // Preserve Advanced's component selection; readiness for Basic is reported independently.
        s.Rfc.Selected=current.Rfc.Selected;s.Adt.Selected=current.Adt.Selected;
        s.BasicSetup = new(DateTimeOffset.UtcNow, adtOnly);
        return s;
    }
}
