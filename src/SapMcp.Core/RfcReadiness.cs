using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SapMcp.Core;

public sealed record RfcReadinessResult(string Code,string Fingerprint,List<Requirement> Checks,List<Requirement> Blockers)
{
    public bool Ready=>Code=="OK";
    public string Summary=>Code+(Blockers.Count==0?"":" · "+string.Join(", ",Blockers.Select(b=>b.Id+": "+(b.Diagnostic?.Code??b.State.ToString()))));
}

public sealed class RfcReadinessService(PackageService packages)
{
    internal static readonly string[] RequiredIds=["python","pyrfc","vc-runtime","vc2013","sdk","rfc-native","rfc-package"];
    public static string Fingerprint(AppSettings s,bool enabled=true)
    {
        object Stamp(string path){var f=new FileInfo(path);return new{path,exists=f.Exists,length=f.Exists?f.Length:0,ticks=f.Exists?f.LastWriteTimeUtc.Ticks:0};}
        var files=new[]{s.NodeExecutable,s.PythonExecutable,s.HostExecutable,Path.Combine(s.PackageDirectory,"rfc","dist","server.js"),Path.Combine(s.PackageDirectory,"rfc","python","worker.py"),Path.Combine(Path.GetDirectoryName(s.PythonExecutable)!,"Lib","site-packages","pyrfc","_cyrfc.cp311-win_amd64.pyd")}
            .Concat(SdkValidation.RequiredDlls.Select(n=>Path.Combine(s.SdkDirectory,"lib",n)))
            .Concat(new[]{"vcruntime140.dll","msvcr120.dll"}.Select(n=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),n)));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{enabled,s.SdkDirectory,s.Rfc.Version,files=files.Select(Stamp)},JsonDefaults.Options))));
    }
    internal static RfcReadinessResult Evaluate(AppSettings s,bool enabled,IEnumerable<Requirement> checks)
    {
        var all=checks.ToList();var blockers=new List<Requirement>();
        if(enabled)foreach(var id in RequiredIds){var check=all.FirstOrDefault(c=>c.Id==id);if(check==null)blockers.Add(new(id,id,"",CheckState.Missing,UiText.Get("Rfc.CheckMissing")));else if(check.State!=CheckState.Ready)blockers.Add(check);}
        if(enabled)blockers.AddRange(all.Where(c=>!RequiredIds.Contains(c.Id)&&c.State is CheckState.Blocked or CheckState.Missing or CheckState.Waiting));
        var code=!enabled?"RFC_DISABLED_BY_MODE":blockers.Any(b=>b.Id=="rfc-native"&&b.State==CheckState.Blocked)?(blockers.First(b=>b.Id=="rfc-native").Detail=="RFC_NATIVE_IMPORT_TIMEOUT"?"RFC_NATIVE_IMPORT_TIMEOUT":"RFC_NATIVE_IMPORT_FAILED"):blockers.Count>0?"RFC_DEPENDENCY_MISSING":"OK";
        return new(code,Fingerprint(s,enabled),all,blockers);
    }
    public async Task<RfcReadinessResult> CheckAsync(AppSettings s,bool enabled=true,CancellationToken ct=default)
    {
        var candidate=BasicPolicy.Copy(s);candidate.Rfc.Selected=enabled;
        var checks=enabled?await new PrerequisiteService(packages).CheckAsync(candidate,ct):[];
        var packagePresent=new[]{Path.Combine(s.PackageDirectory,"rfc","dist","server.js"),Path.Combine(s.PackageDirectory,"rfc","python","worker.py")}.All(File.Exists);
        checks.Add(new("rfc-package","RFC MCP",s.Rfc.Version,packagePresent?CheckState.Ready:CheckState.Missing,UiText.Get("Rfc.PackageCheck")));
        var result=Evaluate(s,enabled,checks);
        string Version(string path){try{return FileVersionInfo.GetVersionInfo(path).ProductVersion??"unknown";}catch{return "unknown";}}
        await new RollingLog(Path.Combine(s.DataDirectory,"logs","manager")).WriteAsync(LogLevel.Info,"rfc.readiness",new{stage="preflight",result.Code,result.Fingerprint,managerVersion=typeof(RfcReadinessService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,hostVersion=Version(s.HostExecutable),blockers=result.Blockers.Select(b=>new{b.Id,state=b.State.ToString(),code=b.Diagnostic?.Code})},ct);
        return result;
    }
    public static string[] Eligible(AppSettings s,ConnectionProfile p,RfcReadinessResult? readiness,bool enabled)=>BasicPolicy.Eligible(s,p).Where(c=>c!="rfc"||readiness is{Ready:true}&&readiness.Fingerprint==Fingerprint(s,enabled)).ToArray();
}

public static class RfcNativeValidation
{
    internal const string Script="""
import json,sys
try:
    from pyrfc import Connection
    assert callable(Connection)
    print(json.dumps(dict(schemaVersion=1,ok=True)))
except Exception:
    print(json.dumps(dict(schemaVersion=1,ok=False)))
    sys.exit(2)
""";
    public static Task<Requirement> CheckAsync(AppSettings s,CancellationToken ct=default)=>CheckAsync(s,(exe,env,dir,token)=>ProcessRunner.RunAsync(exe,["-c",Script],env,TimeSpan.FromSeconds(20),stdin:"",ct:token,workingDirectory:dir),ct);
    internal static async Task<Requirement> CheckAsync(AppSettings s,Func<string,Dictionary<string,string>,string,CancellationToken,Task<ProcessResult>> runner,CancellationToken ct=default)
    {
        var root=Path.GetFullPath(Path.Combine(s.DataDirectory,"cache","rfc-native"));var scratch=Path.Combine(root,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(scratch);
        try{
            var run=await runner(s.PythonExecutable,SdkRuntimeEnvironment.Create(s.SdkDirectory,Environment.GetEnvironmentVariable("PATH")),scratch,ct);
            using var json=JsonDocument.Parse(run.Output);
            if(run.ExitCode==0&&json.RootElement.GetProperty("schemaVersion").GetInt32()==1&&json.RootElement.GetProperty("ok").ValueKind==JsonValueKind.True)return new("rfc-native",UiText.Get("Rfc.NativeName"),"",CheckState.Ready,UiText.Get("Rfc.NativeReady"));
        }catch(OperationCanceledException)when(!ct.IsCancellationRequested){return Failure("RFC_NATIVE_IMPORT_TIMEOUT");}
        catch(OperationCanceledException){throw;}
        catch(Exception ex)when(ex is IOException or JsonException or InvalidOperationException or KeyNotFoundException or System.ComponentModel.Win32Exception){}
        finally{if(Path.GetFullPath(scratch).StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))Directory.Delete(scratch,true);}
        return Failure("RFC_NATIVE_IMPORT_FAILED");
    }
    private static Requirement Failure(string code)=>new("rfc-native",UiText.Get("Rfc.NativeName"),"",CheckState.Blocked,code,Diagnostic:new(code,"pyrfc.import",code=="RFC_NATIVE_IMPORT_TIMEOUT"?"Timeout":"ImportError"));
}
