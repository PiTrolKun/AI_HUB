using System.Diagnostics;

namespace AIHub.Services;

internal sealed record KokoroWorkerFailureDiagnostics(string Error, string StandardError)
{
    internal static async Task<KokoroWorkerFailureDiagnostics> CaptureAsync(
        Process process,
        Task? errorDrainTask,
        Func<string> readStandardError,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // EOF can precede the process exit notification and the last stderr lines.
        // Share one deadline so a child holding a pipe cannot stall speech cleanup.
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
            if (errorDrainTask is not null)
            {
                await errorDrainTask.WaitAsync(deadline.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }
        cancellationToken.ThrowIfCancellationRequested();

        var exit = process.HasExited
            ? $"exitCode={process.ExitCode}; exitCodeHex=0x{unchecked((uint)process.ExitCode):x8}"
            : "exitCode=unavailable; processState=running_after_eof";
        return new KokoroWorkerFailureDiagnostics(
            $"The local Kokoro worker returned no response; launcherPid={process.Id}; {exit}.",
            readStandardError());
    }
}
