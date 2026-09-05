using System.Globalization;

namespace AIHub.Services;

public static class ProductBrand
{
    public static string Name(string? language) =>
        language?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true ? "ЛОПАТА" : "LOPATA";

    // Capture the user's Windows display language independently of app localization.
    public static string WindowsName { get; } = Name(CultureInfo.CurrentUICulture.Name);
}
