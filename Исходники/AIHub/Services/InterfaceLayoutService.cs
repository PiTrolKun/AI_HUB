using System.Windows;
using AIHub.Models;

namespace AIHub.Services;

public enum InterfaceSizeClass
{
    Compact,
    Standard,
    Wide
}

public sealed class InterfaceLayoutService
{
    public InterfaceSizeClass GetSizeClass(double width, double height)
    {
        if (width < 1120 || height < 720)
        {
            return InterfaceSizeClass.Compact;
        }

        return width >= 2100 && height >= 1050
            ? InterfaceSizeClass.Wide
            : InterfaceSizeClass.Standard;
    }

    public double GetAutomaticTypographyScale(double width, double height)
    {
        if (width >= 3000 && height >= 1400)
        {
            return 1.14;
        }

        return width >= 2200 && height >= 1100 ? 1.06 : 1.0;
    }

    public int GetBundleColumnCount(double availableWidth) => availableWidth switch
    {
        < 920 => 1,
        < 1460 => 2,
        _ => 3
    };

    public Rect CalculateHalfScreenBounds(Rect workArea, double minimumWidth, double minimumHeight)
    {
        var width = Math.Clamp(workArea.Width * 0.5, minimumWidth, workArea.Width);
        var height = Math.Clamp(workArea.Height * 0.5, minimumHeight, workArea.Height);
        return new Rect(
            workArea.Left + ((workArea.Width - width) / 2),
            workArea.Top + ((workArea.Height - height) / 2),
            width,
            height);
    }

    public Rect CalculateRememberedBounds(
        RememberedWindowPlacement placement,
        Rect workArea,
        double minimumWidth,
        double minimumHeight)
    {
        if (!placement.HasValue)
        {
            return CalculateHalfScreenBounds(workArea, minimumWidth, minimumHeight);
        }

        var minimumWidthRatio = workArea.Width <= 0 ? 1 : minimumWidth / workArea.Width;
        var minimumHeightRatio = workArea.Height <= 0 ? 1 : minimumHeight / workArea.Height;
        var widthRatio = Math.Clamp(
            NormalizeRatio(placement.WidthRatio, 0.5),
            Math.Min(1, minimumWidthRatio),
            1);
        var heightRatio = Math.Clamp(
            NormalizeRatio(placement.HeightRatio, 0.5),
            Math.Min(1, minimumHeightRatio),
            1);
        var leftRatio = Math.Clamp(
            NormalizeRatio(placement.LeftRatio, (1 - widthRatio) / 2),
            0,
            Math.Max(0, 1 - widthRatio));
        var topRatio = Math.Clamp(
            NormalizeRatio(placement.TopRatio, (1 - heightRatio) / 2),
            0,
            Math.Max(0, 1 - heightRatio));

        return new Rect(
            workArea.Left + (workArea.Width * leftRatio),
            workArea.Top + (workArea.Height * topRatio),
            workArea.Width * widthRatio,
            workArea.Height * heightRatio);
    }

    public RememberedWindowPlacement CapturePlacement(
        Rect normalBounds,
        Rect workArea,
        string monitorDeviceName,
        bool wasMaximized)
    {
        if (workArea.Width <= 0 || workArea.Height <= 0)
        {
            return new RememberedWindowPlacement();
        }

        var widthRatio = Math.Clamp(normalBounds.Width / workArea.Width, 0.05, 1);
        var heightRatio = Math.Clamp(normalBounds.Height / workArea.Height, 0.05, 1);
        return new RememberedWindowPlacement
        {
            HasValue = true,
            MonitorDeviceName = monitorDeviceName,
            LeftRatio = Math.Clamp(
                (normalBounds.Left - workArea.Left) / workArea.Width,
                0,
                Math.Max(0, 1 - widthRatio)),
            TopRatio = Math.Clamp(
                (normalBounds.Top - workArea.Top) / workArea.Height,
                0,
                Math.Max(0, 1 - heightRatio)),
            WidthRatio = widthRatio,
            HeightRatio = heightRatio,
            WasMaximized = wasMaximized
        };
    }

    private static double NormalizeRatio(double value, double fallback) =>
        double.IsFinite(value) ? value : fallback;
}
