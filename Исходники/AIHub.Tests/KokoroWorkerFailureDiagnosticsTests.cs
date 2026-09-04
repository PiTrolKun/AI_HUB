using System.Diagnostics;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class KokoroWorkerFailureDiagnosticsTests
{
    [TestMethod]
    public async Task AbnormalExit_PreservesExitCodeAndDrainedError()
    {
        using var process = Start("echo synthetic-native-stack 1>&2 & exit /b -1073741819");
        var stderr = "";
        var drain = Task.Run(async () => stderr = await process.StandardError.ReadToEndAsync());
        var failure = await KokoroWorkerFailureDiagnostics.CaptureAsync(
            process, drain, () => stderr, CancellationToken.None);
        StringAssert.Contains(failure.Error, "exitCode=-1073741819");
        StringAssert.Contains(failure.Error, "exitCodeHex=0xc0000005");
        StringAssert.Contains(failure.Error, $"launcherPid={process.Id}");
        StringAssert.Contains(failure.StandardError, "synthetic-native-stack");
    }

    [TestMethod]
    public async Task ExitZero_StillReportsMissingResponseWithoutInventingCrash()
    {
        using var process = Start("exit /b 0");
        var failure = await KokoroWorkerFailureDiagnostics.CaptureAsync(
            process, null, () => "", CancellationToken.None);
        StringAssert.Contains(failure.Error, "exitCodeHex=0x00000000");
    }

    [TestMethod]
    public async Task ProcessHoldingPipe_DiagnosticWaitIsBounded()
    {
        using var process = Start("set /p taskInput=");
        try
        {
            var watch = Stopwatch.StartNew();
            var failure = await KokoroWorkerFailureDiagnostics.CaptureAsync(
                process, null, () => "partial stderr", CancellationToken.None);
            StringAssert.Contains(failure.Error, "processState=running_after_eof");
            Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(5));
        }
        finally
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
    }

    [TestMethod]
    public async Task Cancellation_IsNotReportedAsWorkerCrash()
    {
        using var process = Start("set /p taskInput=");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        try
        {
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await KokoroWorkerFailureDiagnostics.CaptureAsync(
                    process, null, () => "", cancellation.Token));
        }
        finally
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
    }

    private static Process Start(string command)
    {
        var info = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        info.ArgumentList.Add("/d");
        info.ArgumentList.Add("/c");
        info.ArgumentList.Add(command);
        return Process.Start(info)!;
    }
}
