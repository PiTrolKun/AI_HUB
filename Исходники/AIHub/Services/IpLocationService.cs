using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class IpLocationService
{
    private static readonly Uri Endpoint = new("https://ipwho.is/?lang=ru");

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    public async Task<UserLocation?> DetectAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(Endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            if (!TryGetBoolean(root, "success"))
            {
                return null;
            }

            return new UserLocation
            {
                Mode = "auto",
                City = GetString(root, "city"),
                Region = GetString(root, "region"),
                Country = GetString(root, "country"),
                CountryCode = GetString(root, "country_code"),
                Timezone = GetTimezone(root),
                Latitude = GetNullableDouble(root, "latitude"),
                Longitude = GetNullableDouble(root, "longitude"),
                Source = "ip",
                UpdatedAt = DateTimeOffset.Now
            };
        }
        catch
        {
            return null;
        }
    }

    private static bool TryGetBoolean(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.True;
    }

    private static string GetString(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string GetTimezone(JsonElement root)
    {
        if (!root.TryGetProperty("timezone", out var timezone))
        {
            return string.Empty;
        }

        if (timezone.ValueKind == JsonValueKind.String)
        {
            return timezone.GetString() ?? string.Empty;
        }

        return timezone.ValueKind == JsonValueKind.Object
            && timezone.TryGetProperty("id", out var id)
            && id.ValueKind == JsonValueKind.String
                ? id.GetString() ?? string.Empty
                : string.Empty;
    }

    private static double? GetNullableDouble(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String
            && double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
        {
            return number;
        }

        return null;
    }
}
