using System.Diagnostics;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class RuntimeResourceDiagnosticsTests
{
    [TestMethod]
    public void Capture_IncludesRootProcessAndPhysicalMemory()
    {
        using var process = Process.GetCurrentProcess();

        var snapshot = RuntimeResourceDiagnostics.Capture(process);

        Assert.AreEqual(process.Id, snapshot.RootProcessId);
        Assert.IsGreaterThanOrEqualTo(1, snapshot.ProcessCount);
        Assert.IsGreaterThan(0, snapshot.WorkingSetBytes);
        Assert.IsGreaterThan(0, snapshot.PrivateBytes);
        Assert.IsGreaterThan(0, snapshot.SystemTotalBytes);
        Assert.IsGreaterThan(0, snapshot.SystemAvailableBytes);
    }

    [TestMethod]
    public void DescribeLaunch_RecordsPlacementAndModelSize()
    {
        using var process = Process.GetCurrentProcess();
        var modelPath = typeof(RuntimeResourceDiagnosticsTests).Assembly.Location;

        var message = RuntimeResourceDiagnostics.DescribeLaunch(
            "test-runtime",
            process,
            "CPU/RAM",
            modelPath);

        StringAssert.Contains(message, "component=test-runtime");
        StringAssert.Contains(message, "placement=CPU/RAM");
        StringAssert.Contains(message, $"pid={process.Id}");
        StringAssert.Contains(message, "modelFileBytes=");
    }

    [TestMethod]
    public void DescribeSystemMemory_RecordsAvailableUsedAndTotalBytes()
    {
        var message = RuntimeResourceDiagnostics.DescribeSystemMemory("test");

        StringAssert.Contains(message, "phase=test");
        StringAssert.Contains(message, "availableBytes=");
        StringAssert.Contains(message, "usedBytes=");
        StringAssert.Contains(message, "totalBytes=");
    }
}
