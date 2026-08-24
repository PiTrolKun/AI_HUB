using System.Windows;
using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class InterfaceLayoutServiceTests
{
    private readonly InterfaceLayoutService _service = new();

    [TestMethod]
    public void SizeClass_UsesAvailableWindowArea()
    {
        Assert.AreEqual(InterfaceSizeClass.Compact, _service.GetSizeClass(980, 640));
        Assert.AreEqual(InterfaceSizeClass.Standard, _service.GetSizeClass(1920, 980));
        Assert.AreEqual(InterfaceSizeClass.Wide, _service.GetSizeClass(2560, 1300));
    }

    [TestMethod]
    [DataRow(900, 1)]
    [DataRow(1200, 2)]
    [DataRow(1800, 3)]
    public void BundleColumns_RespondToAvailableWidth(double width, int expected)
    {
        Assert.AreEqual(expected, _service.GetBundleColumnCount(width));
    }

    [TestMethod]
    public void HalfScreenBounds_AreCenteredInsideWorkArea()
    {
        var result = _service.CalculateHalfScreenBounds(
            new Rect(100, 50, 1920, 1040),
            860,
            560);

        Assert.AreEqual(new Rect(580, 290, 960, 560), result);
    }

    [TestMethod]
    public void RememberedBounds_AreClampedToAvailableMonitor()
    {
        var placement = new RememberedWindowPlacement
        {
            HasValue = true,
            LeftRatio = 4,
            TopRatio = -2,
            WidthRatio = 2,
            HeightRatio = 0.5
        };

        var result = _service.CalculateRememberedBounds(
            placement,
            new Rect(0, 0, 1600, 900),
            860,
            560);

        Assert.AreEqual(new Rect(0, 0, 1600, 560), result);
    }

    [TestMethod]
    public void CaptureAndRestore_UsesMonitorRelativeRatios()
    {
        var captured = _service.CapturePlacement(
            new Rect(500, 250, 1000, 600),
            new Rect(0, 0, 2000, 1000),
            "DISPLAY2",
            wasMaximized: true);

        var restored = _service.CalculateRememberedBounds(
            captured,
            new Rect(100, 100, 1600, 900),
            860,
            560);

        Assert.AreEqual(new Rect(500, 325, 860, 560), restored);
        Assert.IsTrue(captured.WasMaximized);
        Assert.AreEqual("DISPLAY2", captured.MonitorDeviceName);
    }

    [TestMethod]
    public void InterfaceSettings_NormalizeUnsupportedValues()
    {
        var settings = new InterfaceSettings
        {
            TextScalePercent = 500,
            WindowStartupMode = "unknown"
        };

        Assert.AreEqual(150, settings.TextScalePercent);
        Assert.AreEqual(WindowStartupModes.RememberLast, settings.WindowStartupMode);
    }
}
