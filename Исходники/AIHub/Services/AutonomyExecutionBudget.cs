using System.Diagnostics;
using AIHub.Models;

namespace AIHub.Services;

public sealed class AutonomyExecutionBudget
{
    private readonly Stopwatch _stopwatch = new();
    private readonly TimeSpan _limit;
    private string _lastFingerprint = string.Empty;
    private int _sameFingerprintCount;

    public AutonomyExecutionBudget(int seconds)
    {
        _limit = TimeSpan.FromSeconds(Math.Clamp(
            seconds,
            CoreAutonomySettings.MinimumSeconds,
            CoreAutonomySettings.MaximumSeconds));
    }

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public TimeSpan Limit => _limit;

    public void Start()
    {
        if (!_stopwatch.IsRunning)
        {
            _stopwatch.Start();
        }
    }

    public bool CanStartNext(bool isFirstOperation = false) =>
        isFirstOperation
        || !_stopwatch.IsRunning
        || _stopwatch.Elapsed < _limit;

    public bool RegisterProgress(string fingerprint)
    {
        var normalized = fingerprint.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            _lastFingerprint = string.Empty;
            _sameFingerprintCount = 0;
            return true;
        }

        if (string.Equals(normalized, _lastFingerprint, StringComparison.Ordinal))
        {
            _sameFingerprintCount++;
        }
        else
        {
            _lastFingerprint = normalized;
            _sameFingerprintCount = 0;
        }

        return _sameFingerprintCount < 2;
    }
}
