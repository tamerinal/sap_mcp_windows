using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using SapMcp.Core;
using Xunit;

namespace SapMcp.Tests;

public sealed class SdkTests
{
    private static string Success => string.Join("\n", new[] { "icu.load", "icu.init", "pyrfc.import", "sdk.version", "sdk.init" }.Select(stage => JsonSerializer.Serialize(new { schemaVersion = 1, stage })))
        + "\n{\"schemaVersion\":1,\"stage\":\"complete\",\"ok\":true,\"version\":\"7500.0.18\"}";

    [Fact] public void ChildPathIsPrependedDeduplicatedAndParentRemainsUnchanged()
    {
        using var temp = new TestDirectory(); var sdk = Path.Combine(temp.Root, "SDK Türkçe boşluk"); var lib = Path.Combine(sdk, "lib");
        var inherited = "C:\\Windows\\System32;\"" + lib.ToUpperInvariant() + "\\\";C:\\Other;" + lib;
        var parent = Environment.GetEnvironmentVariable("PATH");
        var result = SdkRuntimeEnvironment.Create(sdk, inherited);
        Assert.Equal(lib + ";C:\\Windows\\System32;C:\\Other", result["PATH"]);
        Assert.Equal(sdk, result["SAPNWRFC_HOME"]); Assert.Equal("utf-8", result["PYTHONIOENCODING"]);
        Assert.Equal(parent, Environment.GetEnvironmentVariable("PATH"));
        var start = new ProcessStartInfo(); start.Environment["PATH"] = inherited; SdkRuntimeEnvironment.Apply(start, sdk);
        Assert.Equal(result["PATH"], start.Environment["PATH"]);
    }

    [Fact] public void EmptySdkDoesNotChangeInheritedPath()
    {
        var result = SdkRuntimeEnvironment.Create("", "unchanged");
        Assert.False(result.ContainsKey("PATH")); Assert.False(result.ContainsKey("SAPNWRFC_HOME"));
    }

    [Fact] public void RfcAndRfcEnabledAdtUseSameEnvironmentButStandaloneAdtKeepsItsPath()
    {
        using var temp = new TestDirectory(); var s = AppSettings.CreateDefault(temp.Root);
        s.SdkDirectory = Path.Combine(temp.Root, "SDK Türkçe boşluk");
        Directory.CreateDirectory(Path.GetDirectoryName(s.NodeExecutable)!); File.WriteAllText(s.NodeExecutable, "fixture");
        foreach (var component in new[] { "rfc", "adt" })
        {
            s.Component(component).Installed = true;
            var dist = Path.Combine(s.PackageDirectory, component, "dist"); Directory.CreateDirectory(dist); File.WriteAllText(Path.Combine(dist, "server.js"), "fixture");
        }
        var rfc = LaunchConfiguration.Create(s, "rfc", true); var adt = LaunchConfiguration.Create(s, "adt", true);
        Assert.Equal(rfc.Environment["PATH"], adt.Environment["PATH"]);
        Assert.Equal(s.SdkDirectory, rfc.Environment["SAPNWRFC_HOME"]);
        Assert.Equal(Environment.GetEnvironmentVariable("PATH"), LaunchConfiguration.Create(s, "adt", false).Environment["PATH"]);
    }

    [Fact] public void SuccessRequiresCompleteStructuredProbeAndZeroExit()
    {
        var check = SdkValidation.Evaluate(new(0, Success, "password=DO_NOT_DISPLAY"));
        Assert.Equal(CheckState.Ready, check.State); Assert.Equal("7500.0.18", check.Version);
        Assert.DoesNotContain("DO_NOT_DISPLAY", check.Detail);
        Assert.Equal(CheckState.Blocked, SdkValidation.Evaluate(new(-1, Success, "")).State);
    }

    [Theory]
    [InlineData(0, "")]
    [InlineData(0, "{}")]
    [InlineData(0, "noise")]
    [InlineData(0, "{\"schemaVersion\":1,\"stage\":\"complete\",\"ok\":true,\"version\":\"7500.0.18\"}")]
    [InlineData(0, "{\"schemaVersion\":1,\"stage\":\"icu.load\"}")]
    [InlineData(0, "{\"schemaVersion\":1,\"stage\":\"complete\"}\n{\"schemaVersion\":1,\"stage\":\"complete\",\"ok\":true,\"version\":\"7500.0.18\"}")]
    [InlineData(-1073741515, "")]
    public void CrashMissingOrMalformedResultCannotBeReady(int exit, string output)
    {
        var check = SdkValidation.Evaluate(new(exit, output, "token=DO_NOT_DISPLAY"));
        Assert.Equal(CheckState.Blocked, check.State); Assert.NotNull(check.Diagnostic); Assert.True(check.Diagnostic.IsSafe);
        Assert.Equal(exit, check.Diagnostic.ExitCode); Assert.DoesNotContain("DO_NOT_DISPLAY", check.Detail);
        if (exit == -1073741515) Assert.Equal("0xC0000135", check.Diagnostic.ExitCodeHex);
    }

    [Fact] public void NativeFailurePreservesLastStageAndUnsignedHex()
    {
        var output = string.Join("\n", Success.Split('\n').Take(5));
        var check = SdkValidation.Evaluate(new(-1, output, ""));
        Assert.Equal("sdk.init", check.Diagnostic!.Stage); Assert.Equal("0xFFFFFFFF", check.Diagnostic.ExitCodeHex);
        Assert.Equal("sdkProbe", check.Diagnostic.Source); Assert.Null(check.Diagnostic.Signal);
    }

    [Fact] public void FailedOrUntrustedProbeCannotOverrideSafetyBoundary()
    {
        var bad = Success + "\n{\"schemaVersion\":1,\"stage\":\"password=SECRET\"}";
        Assert.Equal(CheckState.Blocked, SdkValidation.Evaluate(new(0, bad, "")).State);
        Assert.DoesNotContain("SECRET", SdkValidation.Evaluate(new(0, bad, "")).Detail);
        Assert.Null(McpSession.ReadHostDiagnostic("{\"code\":\"SAP_MCP_START_FAILED\",\"message\":\"SECRET\",\"diagnostic\":{\"code\":\"SECRET\",\"stage\":\"start\",\"errorClass\":\"ProcessExit\"}}"));
        Assert.Null(McpSession.ReadHostDiagnostic("{\"code\":\"SAP_MCP_START_FAILED\",\"diagnostic\":{\"code\":null,\"stage\":null,\"errorClass\":null}}"));
        var trusted = JsonSerializer.Serialize(new { code = "SAP_MCP_START_FAILED", message = "SECRET", diagnostic = new ProcessDiagnostic("SDK_PROBE_FAILED", "sdk.init", "ProcessExit", -1) }, JsonDefaults.Options);
        var diagnostic = McpSession.ReadHostDiagnostic(trusted)!; Assert.True(diagnostic.IsSafe); Assert.DoesNotContain("SECRET", diagnostic.Summary);
    }

    [Fact] public async Task MissingIcuBlocksBeforeAnyPythonStarts()
    {
        using var temp = new TestDirectory(); var s = Prepare(temp); File.Delete(Path.Combine(s.SdkDirectory, "lib", "icuuc57.dll"));
        var check = await SdkValidation.CheckAsync(s, (_, _, _, _) => throw new Exception("Python must not start"));
        Assert.Equal("SDK_DLL_MISSING", check.Diagnostic!.Code); Assert.Contains("icuuc57.dll", check.Detail);
    }

    [Fact] public async Task WrongArchitectureAndMalformedDllAreBlocked()
    {
        using var temp = new TestDirectory(); var s = Prepare(temp); var file = Path.Combine(s.SdkDirectory, "lib", "icuin57.dll");
        WritePe(file, Machine.I386);
        Assert.Equal("SDK_ARCH_MISMATCH", (await SdkValidation.CheckAsync(s, (_, _, _, _) => throw new Exception())).Diagnostic!.Code);
        await File.WriteAllTextAsync(file, "not a PE");
        Assert.Equal("SDK_DLL_INVALID", (await SdkValidation.CheckAsync(s, (_, _, _, _) => throw new Exception())).Diagnostic!.Code);
    }

    [Fact] public async Task ProbeUsesSavedSdkAndAnEmptyOwnedWorkingDirectory()
    {
        using var temp = new TestDirectory(); var s = Prepare(temp); string? working = null;
        var check = await SdkValidation.CheckAsync(s, (exe, env, directory, _) =>
        {
            working = directory; Assert.Equal(s.PythonExecutable, exe); Assert.Empty(Directory.EnumerateFileSystemEntries(directory));
            Assert.Equal(s.SdkDirectory, env["SAPNWRFC_HOME"]); Assert.StartsWith(Path.Combine(s.SdkDirectory, "lib") + ";", env["PATH"]);
            return Task.FromResult(new ProcessResult(0, Success, ""));
        });
        Assert.Equal(CheckState.Ready, check.State); Assert.False(Directory.Exists(working));
    }

    [Fact] public async Task TimeoutIsBlockedAndUserCancellationPropagates()
    {
        using var temp = new TestDirectory(); var s = Prepare(temp);
        var check = await SdkValidation.CheckAsync(s, (_, _, _, _) => throw new OperationCanceledException());
        Assert.Equal("SDK_PROBE_TIMEOUT", check.Diagnostic!.Code);
        using var stop = new CancellationTokenSource(); stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SdkValidation.CheckAsync(s, (_, _, _, _) => throw new Exception(), stop.Token));
    }

    private static AppSettings Prepare(TestDirectory temp)
    {
        var s = AppSettings.CreateDefault(temp.Root); s.SdkDirectory = Path.Combine(temp.Root, "SDK Türkçe boşluk");
        Directory.CreateDirectory(Path.Combine(s.SdkDirectory, "lib"));
        foreach (var name in SdkValidation.RequiredDlls) WritePe(Path.Combine(s.SdkDirectory, "lib", name), Machine.Amd64);
        Directory.CreateDirectory(Path.GetDirectoryName(s.PythonExecutable)!); File.WriteAllText(s.PythonExecutable, "test runner seam");
        return s;
    }

    private static void WritePe(string file, Machine machine)
    {
        var builder = new ManagedPEBuilder(new PEHeaderBuilder(machine: machine, imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll),
            new MetadataRootBuilder(new MetadataBuilder()), new BlobBuilder());
        var blob = new BlobBuilder(); builder.Serialize(blob); File.WriteAllBytes(file, blob.ToArray());
    }
}
