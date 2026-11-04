using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using SapMcp.Core;

namespace SapMcp.Integration;

internal static class OpenCodeAcceptance
{
    public static async Task RunAsync(string publish,string payload,string output,string cli1,string cli2,string sdk)
    {
        output=Path.GetFullPath(output);Directory.CreateDirectory(output);var results=new List<object>();
        var root=Path.Combine(Path.GetTempPath(),"SapMcp OpenCode "+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);await File.WriteAllTextAsync(Path.Combine(output,"fixture-root.local.txt"),root);
        try
        {
            for(var parent=Directory.GetParent(root);parent!=null;parent=parent.Parent)
                if(new[]{"opencode.json","opencode.jsonc",".opencode/opencode.json",".opencode/opencode.jsonc"}.Any(n=>File.Exists(Path.Combine(parent.FullName,n))))throw new InvalidOperationException("OpenCode fixture has ancestor configuration");
            if(Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"opencode")))throw new InvalidOperationException("Managed OpenCode configuration prevents isolated acceptance");
            var s=AppSettings.CreateDefault(Path.Combine(root,"manager"));s.HostExecutable=Path.Combine(Path.GetFullPath(publish),"SapMcpHost.exe");s.SdkDirectory=Path.GetFullPath(sdk);
            s.LandscapeFile=Path.Combine(s.DataDirectory,"landscape.xml");Directory.CreateDirectory(s.DataDirectory);
            await File.WriteAllTextAsync(s.LandscapeFile,ManagedJsonConnectionSource.ExportXml(new()));s.Adt.LogLevel=s.Rfc.LogLevel=LogLevel.Off;
            var packages=new PackageService(Path.GetFullPath(payload));
            await new PrerequisiteService(packages).InstallAsync(s,["node","python","pyrfc"],new Progress<string>(Console.WriteLine));
            await new DeploymentService(packages,new PrerequisiteService(packages)).InstallSelectedAsync(s);
            await ProfileService.MaterializeAsync(s);var config=Path.Combine(s.DataDirectory,"config","settings.json");await new SettingsStore(config).SaveAsync(s);
            foreach(var (version,cli) in new[]{(OpenCodeVersion.V1,Path.GetFullPath(cli1)),(OpenCodeVersion.V2,Path.GetFullPath(cli2))})
            foreach(var scope in new[]{"global","project"})
            {
                var fixture=Path.Combine(root,version+"-"+scope);var work=Path.Combine(fixture,"project");var home=Path.Combine(fixture,"home");Directory.CreateDirectory(work);Directory.CreateDirectory(home);
                var configHome=Path.Combine(home,".config");var configDir=scope=="global"?Path.Combine(configHome,"opencode"):work;Directory.CreateDirectory(configDir);
                var target=Path.Combine(configDir,"opencode.jsonc");await File.WriteAllTextAsync(target,"{ // isolated acceptance fixture\n \"instructions\":[\"./fixture-rules.md\"],\n}");
                await File.WriteAllTextAsync(Path.Combine(configDir,"fixture-rules.md"),"List MCP servers only. Do not call SAP tools.");
                await ClientIntegration.ApplyAsync(ClientIntegration.PreviewOpenCode(target,s,config,["adt","rfc"],version),false);
                var environment=new Dictionary<string,string>{["HOME"]=home,["USERPROFILE"]=home,["APPDATA"]=Path.Combine(home,"AppData","Roaming"),["LOCALAPPDATA"]=Path.Combine(home,"AppData","Local"),["XDG_CONFIG_HOME"]=configHome,["XDG_DATA_HOME"]=Path.Combine(home,".local","share"),["XDG_CACHE_HOME"]=Path.Combine(home,".cache"),["XDG_STATE_HOME"]=Path.Combine(home,".local","state"),["OPENCODE_DISABLE_AUTOUPDATE"]="1",["OPENCODE_DISABLE_MODELS_FETCH"]="1",["OPENCODE_CONFIG"]="",["OPENCODE_CONFIG_CONTENT"]="",["OPENCODE_CONFIG_DIR"]=Path.Combine(configHome,"opencode"),["OPENCODE_TEST_HOME"]=home,["NO_COLOR"]="1"};
                await using var server=version==OpenCodeVersion.V2?await V2Server.StartAsync(cli,environment,work):null;
                var args=server==null?new[]{"mcp","list"}:new[]{"--server",server.Url,"mcp","list"};
                var run=new ProcessResult(1,"","");var text="";
                for(var attempt=0;attempt<15;attempt++)
                {
                    run=await ProcessRunner.RunAsync(cli,args,environment,TimeSpan.FromSeconds(40),workingDirectory:work);
                    text=Regex.Replace(run.Output+"\n"+run.Error,@"\x1B\[[0-?]*[ -/]*[@-~]","");
                    if(run.ExitCode==0 && new[]{"sap_adt_mcp","sap_rfc_call"}.All(n=>text.Contains(n,StringComparison.Ordinal)) && Regex.Matches(text,"connected",RegexOptions.IgnoreCase).Count>=2)break;
                    if(server==null)break;await Task.Delay(500);
                }
                await File.WriteAllTextAsync(Path.Combine(output,version+"-"+scope+".private.log"),run.Output+"\n"+run.Error);
                if(run.ExitCode!=0 || !new[]{"sap_adt_mcp","sap_rfc_call"}.All(n=>text.Contains(n,StringComparison.Ordinal)) || Regex.Matches(text,"connected",RegexOptions.IgnoreCase).Count<2)
                {
                    if(server!=null)
                    {
                        var debug=await ProcessRunner.RunAsync(cli,["--server",server.Url,"debug","config"],environment,TimeSpan.FromMinutes(1),workingDirectory:work);
                        await File.WriteAllTextAsync(Path.Combine(output,"V2-config.private.log"),debug.Output+"\n"+debug.Error);
                    }
                    throw new InvalidOperationException("OpenCode "+version+" "+scope+" did not connect both fixture MCP servers; inspect private acceptance log");
                }
                if(ClientIntegration.PreviewOpenCode(target,s,config,["adt","rfc"],version).Changed)throw new InvalidOperationException("OpenCode changed the generated registration");
                results.Add(new{version=version.ToString(),scope,status="passed",components=2});Console.WriteLine("PASS real OpenCode "+version+" "+scope+": both MCP servers connected; no SAP call");
            }
            await File.WriteAllTextAsync(Path.Combine(output,"report.json"),JsonSerializer.Serialize(results,JsonDefaults.Options));
        }
        finally
        {
            var temporaryRoot=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(!Path.GetFullPath(root).StartsWith(temporaryRoot,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Invalid fixture cleanup path");
            for(var attempt=0;attempt<8 && results.Count==4 && Directory.Exists(root);attempt++)
            {
                try { Directory.Delete(root,true); }
                catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){await Task.Delay(250*(attempt+1));}
            }
            if(Directory.Exists(root))Console.WriteLine("NOTE OpenCode fixture cleanup pending; a CLI database is still closing.");
        }
    }
    private sealed class V2Server(Process process,string url,Task<string> output,Task<string> error):IAsyncDisposable
    {
        public string Url {get;}=url;
        public static async Task<V2Server> StartAsync(string cli,Dictionary<string,string> environment,string work)
        {
            environment["OPENCODE_PASSWORD"]=Guid.NewGuid().ToString("N");
            var start=new ProcessStartInfo(cli){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=work};
            foreach(var arg in new[]{"serve","--stdio","--port","0"})start.ArgumentList.Add(arg);
            foreach(var pair in environment)start.Environment[pair.Key]=pair.Value;
            var process=Process.Start(start)??throw new IOException("Could not start isolated OpenCode V2 server");
            var error=process.StandardError.ReadToEndAsync();
            try
            {
                var line=await process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(60));
                using var ready=JsonDocument.Parse(line??"");var url=ready.RootElement.GetProperty("url").GetString()!;
                return new(process,url,process.StandardOutput.ReadToEndAsync(),error);
            }
            catch { if(!process.HasExited)process.Kill(true);process.Dispose();throw; }
        }
        public async ValueTask DisposeAsync()
        {
            process.StandardInput.Close();
            try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)); }
            catch(TimeoutException){process.Kill(true);await process.WaitForExitAsync();}
            await Task.WhenAll(output,error);process.Dispose();
        }
    }

}
