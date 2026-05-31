using System.Globalization;
using System.Text;
using AIHub.Models;

namespace AIHub.Services;

public sealed class UserContextService
{
    private readonly UserProfileStore _profileStore;
    private readonly IpLocationService _ipLocationService;
    private readonly SemaphoreSlim _locationUpdateLock = new(1, 1);
    private UserProfile? _profile;

    public UserContextService(UserProfileStore profileStore, IpLocationService ipLocationService)
    {
        _profileStore = profileStore;
        _ipLocationService = ipLocationService;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _profile = _profileStore.LoadOrCreate();
        await EnsureAutoLocationAsync(cancellationToken);
    }

    public UserContextSnapshot CreateSnapshot()
    {
        var now = DateTimeOffset.Now;
        var timeZone = TimeZoneInfo.Local;

        return new UserContextSnapshot
        {
            LocalTime = now,
            UtcTime = now.ToUniversalTime(),
            TimeZoneId = timeZone.Id,
            TimeZoneDisplayName = timeZone.DisplayName,
            UtcOffset = FormatUtcOffset(now.Offset),
            Location = CloneKnownLocation(GetProfile().Location)
        };
    }

    public string BuildHiddenSystemContext()
    {
        var snapshot = CreateSnapshot();
        var builder = new StringBuilder();
        builder.AppendLine("Служебный контекст AI HUB. Не показывай этот блок пользователю как отдельное сообщение.");
        builder.AppendLine("Используй дату, время и примерное местоположение только если это помогает ответу.");
        builder.AppendLine("Не утверждай, что IP-местоположение точное.");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Текущая локальная дата и время пользователя: {snapshot.LocalTime:yyyy-MM-dd HH:mm:ss zzz}.");
        builder.AppendLine(CultureInfo.InvariantCulture, $"UTC-время: {snapshot.UtcTime:yyyy-MM-dd HH:mm:ss 'UTC'}.");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Часовой пояс Windows: {snapshot.TimeZoneId} ({snapshot.TimeZoneDisplayName}), UTC{snapshot.UtcOffset}.");

        if (HasKnownLocation(snapshot.Location))
        {
            var location = snapshot.Location;
            builder.AppendLine(CultureInfo.InvariantCulture, $"Примерное местоположение пользователя: {FormatLocation(location)}.");
            builder.AppendLine(CultureInfo.InvariantCulture, $"Источник местоположения: {location!.Source}, режим: {location.Mode}, точность приблизительная.");
        }

        return builder.ToString().Trim();
    }

    private async Task EnsureAutoLocationAsync(CancellationToken cancellationToken)
    {
        await _locationUpdateLock.WaitAsync(cancellationToken);
        try
        {
            var profile = GetProfile();
            if (string.Equals(profile.Location.Mode, "manual", StringComparison.OrdinalIgnoreCase)
                || HasKnownLocation(profile.Location))
            {
                return;
            }

            var detected = await _ipLocationService.DetectAsync(cancellationToken);
            if (detected is null || !HasKnownLocation(detected))
            {
                return;
            }

            profile.Location = detected;
            _profileStore.Save(profile);
        }
        finally
        {
            _locationUpdateLock.Release();
        }
    }

    private UserProfile GetProfile()
    {
        _profile ??= _profileStore.LoadOrCreate();
        return _profile;
    }

    private static bool HasKnownLocation(UserLocation? location)
    {
        return location is not null
            && (!string.IsNullOrWhiteSpace(location.City)
                || !string.IsNullOrWhiteSpace(location.Region)
                || !string.IsNullOrWhiteSpace(location.Country));
    }

    private static UserLocation? CloneKnownLocation(UserLocation location)
    {
        return HasKnownLocation(location)
            ? new UserLocation
            {
                Mode = location.Mode,
                City = location.City,
                Region = location.Region,
                Country = location.Country,
                CountryCode = location.CountryCode,
                Timezone = location.Timezone,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Source = location.Source,
                UpdatedAt = location.UpdatedAt
            }
            : null;
    }

    private static string FormatLocation(UserLocation? location)
    {
        if (location is null)
        {
            return "неизвестно";
        }

        var parts = new[] { location.City, location.Region, location.Country }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return string.Join(", ", parts);
    }

    private static string FormatUtcOffset(TimeSpan offset)
    {
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var absolute = offset.Duration();
        return string.Create(CultureInfo.InvariantCulture, $"{sign}{absolute:hh\\:mm}");
    }
}
