using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SapMcp.Core;

public sealed record BasicRfcServer(string Host,string Sysnr);
public sealed record BasicRfcServers(string Code,List<BasicRfcServer> Servers)
{
    public bool Available=>Code=="OK"&&Servers.Count>0;
}

public sealed class BasicDiscoveryService
{
    private static readonly JsonSerializerOptions InputJson=new(JsonDefaults.Options){DefaultIgnoreCondition=JsonIgnoreCondition.WhenWritingNull};
    internal static string SerializeRequest(BasicDraft draft,string mode="smart",IReadOnlyList<BasicRfcServer>? servers=null)
    {
        var (host,port)=BasicPolicy.ParseHost(draft.Host);
        var manual=BasicPolicy.ManualUrl(draft.ManualUrl,draft.Host);
        var preferred=string.IsNullOrWhiteSpace(draft.PreferredUrl)?null:AdtProfiles.NormalizeUrl(draft.PreferredUrl);
        if(preferred!=null&&!new Uri(preferred).Host.Trim('[',']').Equals(host,StringComparison.OrdinalIgnoreCase))preferred=null;
        var input=new {host,sysnr=draft.Sysnr.Length==2?draft.Sysnr:null,preferredUrl=preferred,explicitPort=port,
            manualUrl=manual.Length==0?null:manual,mode,servers,options=new{concurrency=BasicPolicy.Discovery.Concurrency,probeTimeoutMs=BasicPolicy.Discovery.ProbeTimeoutMs,budgetMs=BasicPolicy.Discovery.BudgetMs,tcpTimeoutMs=BasicPolicy.Discovery.TcpTimeoutMs,tcpBudgetMs=BasicPolicy.Discovery.TcpBudgetMs}};
        return JsonSerializer.Serialize(input,InputJson);
    }
    internal static BasicDiscovery ReadResponse(ProcessResult run)
    {
        try {
            using var json=JsonDocument.Parse(run.Output);
            if(run.ExitCode!=0){var raw=json.RootElement.TryGetProperty("code",out var c)?c.GetString():null;throw new BasicDiscoveryException(raw is "DISCOVERY_PORT_INVALID" or "DISCOVERY_HOST_INVALID" or "DISCOVERY_OPTIONS_INVALID" or "DISCOVERY_INPUT_INVALID"?raw:"DISCOVERY_PROCESS_FAILED");}
            var r=json.RootElement.Deserialize<BasicDiscovery>(JsonDefaults.Options);
            if(r is not {SchemaVersion:2}||r.Candidates==null||r.Candidates.Count>131073)throw new InvalidDataException();
            return r;
        }catch(Exception ex)when(ex is JsonException or InvalidDataException or InvalidOperationException){throw new BasicDiscoveryException("DISCOVERY_OUTPUT_INVALID");}
    }
    public Task<BasicDiscovery> DiscoverAsync(AppSettings s,BasicDraft draft,CancellationToken ct=default)=>DiscoverAsync(s,draft,"smart",[],null,ct);
    public async Task<BasicDiscovery> DiscoverAsync(AppSettings s,BasicDraft draft,string mode,IReadOnlyList<BasicRfcServer> servers,IProgress<string>? progress,CancellationToken ct=default)
    {
        var entry=Path.Combine(s.PackageDirectory,"adt","dist","basic-connection.js");
        if(!File.Exists(entry)||!File.Exists(s.NodeExecutable))throw new BasicDiscoveryException("DISCOVERY_PACKAGE_MISSING");
        try {
            using var metadata=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(s.PackageDirectory,"adt","build-info.json"),ct));
            if(!metadata.RootElement.TryGetProperty("basicDiscoveryVersion",out var v)||v.GetInt32()!=2)throw new BasicDiscoveryException("BASIC_DISCOVERY_UPGRADE_REQUIRED");
        }catch(Exception ex)when(ex is IOException or JsonException or InvalidOperationException && ex is not BasicDiscoveryException){throw new BasicDiscoveryException("BASIC_DISCOVERY_UPGRADE_REQUIRED");}
        var (warmHost,warmPort)=BasicPolicy.ParseHost(draft.Host);
        // Cold route/first-packet delay can exceed the per-port TCP timeout and mark open ports closed; warm the path best-effort before scanning.
        await WarmUpRouteAsync(warmHost,BasicWarmPorts(draft,warmPort),ct);
        var start=new ProcessStartInfo(s.NodeExecutable){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardInputEncoding=new UTF8Encoding(false),StandardOutputEncoding=Encoding.UTF8};
        start.ArgumentList.Add(entry);
        using var process=Process.Start(start)??throw new BasicDiscoveryException("DISCOVERY_PROCESS_FAILED");
        using var limit=CancellationTokenSource.CreateLinkedTokenSource(ct);limit.CancelAfter(TimeSpan.FromSeconds(100));
        // Drain, never display or retain upstream stderr (it can contain network/account information).
        var stderr=Task.Run(async()=>{var buffer=new char[4096];while(await process.StandardError.ReadAsync(buffer,limit.Token)>0){}},CancellationToken.None);
        BasicDiscovery? result=null;var hello=false;
        try {
            await process.StandardInput.WriteAsync(SerializeRequest(draft,mode,servers).AsMemory(),limit.Token);process.StandardInput.Close();
            while(await ReadLineAsync(process.StandardOutput,limit.Token) is {} line){
                using var json=JsonDocument.Parse(line);var e=json.RootElement;
                if(e.GetProperty("schemaVersion").GetInt32()!=2)throw new BasicDiscoveryException("BASIC_DISCOVERY_UPGRADE_REQUIRED");
                switch(e.GetProperty("type").GetString()){
                    case "hello":hello=true;break;
                    case "error":throw new BasicDiscoveryException(BasicPolicy.SafeCode(e.GetProperty("code").GetString()));
                    case "progress":
                        var phase=e.GetProperty("phase").GetString();var completed=e.GetProperty("completed").GetInt32();var total=e.GetProperty("total").GetInt32();
                        if(completed<0||completed>total||total>131072)throw new InvalidDataException();
                        progress?.Report(UiText.Get(phase=="tcp"?"Basic.TcpProgress":"Basic.HttpProgress",completed,total));break;
                    case "completed":if(result!=null)throw new InvalidDataException();result=ReadResponse(new(0,e.GetProperty("result").GetRawText(),""));break;
                }
            }
            await process.WaitForExitAsync(limit.Token);await stderr;
            if(!hello||process.ExitCode!=0||result==null)throw new BasicDiscoveryException("DISCOVERY_OUTPUT_INVALID");
            ValidateTargets(draft,result,mode,servers);
            await new RollingLog(Path.Combine(s.DataDirectory,"logs","manager")).WriteAsync(LogLevel.Info,"basic.discovery",new{result.DiscoveryId,result.BudgetExpired,result.DurationMs,candidates=result.Candidates.Count},ct);
            if(result.BudgetExpired)progress?.Report(UiText.Get("Basic.ScanIncomplete"));
            return result;
        }catch(OperationCanceledException)when(!ct.IsCancellationRequested){throw new BasicDiscoveryException("DISCOVERY_PROCESS_TIMEOUT");}
        catch(JsonException){throw new BasicDiscoveryException("DISCOVERY_OUTPUT_INVALID");}
        finally {limit.Cancel();if(!process.HasExited)process.Kill(true);await process.WaitForExitAsync(CancellationToken.None);try{await stderr;}catch(OperationCanceledException){}}
    }
    internal static IReadOnlyList<int> BasicWarmPorts(BasicDraft draft,int? explicitPort)
    {
        var n=draft.Sysnr.Length==2&&int.TryParse(draft.Sysnr,out var sysnr)?sysnr:0;
        var ports=new List<int>{44300+n,8080};
        if(explicitPort is int p)ports.Insert(0,p);
        return ports;
    }
    internal static async Task WarmUpRouteAsync(string host,IReadOnlyList<int> ports,CancellationToken ct=default)
    {
        foreach(var port in ports){
            try {
                using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(1));
                using var client=new TcpClient();
                await client.ConnectAsync(host,port,timeout.Token);
            }
            catch(OperationCanceledException)when(!ct.IsCancellationRequested){} // Attempt itself timed out; the route is likely warmed, outcome is irrelevant.
            catch(Exception ex)when(ex is SocketException or ObjectDisposedException or InvalidOperationException){} // Best-effort: refusal, DNS failure and disposal are acceptable warm-up outcomes.
        }
    }
    internal static void ValidateTargets(BasicDraft draft,BasicDiscovery result,string mode,IReadOnlyList<BasicRfcServer> servers)
    {
        var host=BasicPolicy.ParseHost(draft.Host).Host;var manual=BasicPolicy.ManualUrl(draft.ManualUrl,draft.Host);
        var hosts=new HashSet<string>(StringComparer.OrdinalIgnoreCase){host};if(mode=="rfc")foreach(var server in servers)hosts.Add(BasicPolicy.ParseHost(server.Host).Host);
        foreach(var candidate in result.Candidates){var normalized=AdtProfiles.NormalizeUrl(candidate.Url);var u=new Uri(normalized);if(manual.Length>0?normalized!=manual:!hosts.Contains(u.Host.Trim('[',']')))throw new InvalidDataException(UiText.Get("Basic.DiscoveryInvalid"));}
    }
    private static async Task<string?> ReadLineAsync(StreamReader reader,CancellationToken ct)
    {
        var text=new StringBuilder();var buffer=new char[1];
        while(await reader.ReadAsync(buffer,ct)>0){if(buffer[0]=='\n')return text.ToString();if(text.Length>=32*1024*1024)throw new BasicDiscoveryException("DISCOVERY_OUTPUT_INVALID");text.Append(buffer[0]);}
        return text.Length==0?null:text.ToString();
    }
}
