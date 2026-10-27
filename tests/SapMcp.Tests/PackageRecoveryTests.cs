using SapMcp.Core;
using Xunit;

namespace SapMcp.Tests;
public sealed class PackageRecoveryTests
{
    [Fact] public async Task TransientDirectoryLockRetriesThenMovesTheActualDirectory()
    {
        using var t=new TestDirectory();var from=Path.Combine(t.Root,"stage");var to=Path.Combine(t.Root,"installed");Directory.CreateDirectory(from);await File.WriteAllTextAsync(Path.Combine(from,"file"),"verified");var attempts=0;var delays=new List<int>();
        await PackageService.RetryFileOperationAsync(()=>{if(++attempts<3)throw new IOException("fixture",unchecked((int)0x80070005));Directory.Move(from,to);},CancellationToken.None,(ms,ct)=>{delays.Add(ms);return Task.CompletedTask;});
        Assert.Equal(3,attempts);Assert.Equal(new[]{100,200},delays);Assert.Equal("verified",await File.ReadAllTextAsync(Path.Combine(to,"file")));
    }
    [Fact] public async Task PersistentLockIsBoundedAndUnrelatedIoIsNotRetried()
    {
        var attempts=0;await Assert.ThrowsAsync<IOException>(()=>PackageService.RetryFileOperationAsync(()=>{attempts++;throw new IOException("fixture",unchecked((int)0x80070020));},CancellationToken.None,(_,_)=>Task.CompletedTask));Assert.Equal(6,attempts);
        attempts=0;await Assert.ThrowsAsync<IOException>(()=>PackageService.RetryFileOperationAsync(()=>{attempts++;throw new IOException("bad path",unchecked((int)0x80070003));},CancellationToken.None,(_,_)=>Task.CompletedTask));Assert.Equal(1,attempts);
    }
    [Fact] public async Task DirectoryRetryHonorsCancellationWithoutMoreAttempts()
    {
        using var c=new CancellationTokenSource();var attempts=0;await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>PackageService.RetryFileOperationAsync(()=>{attempts++;throw new IOException("fixture",unchecked((int)0x80070005));},c.Token,(_,ct)=>{c.Cancel();return Task.FromCanceled(ct);}));Assert.Equal(1,attempts);
    }
}
