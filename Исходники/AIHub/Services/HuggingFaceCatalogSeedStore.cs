using System.IO;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public static class HuggingFaceCatalogSeedStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static HuggingFaceCatalogSeed Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var json = File.ReadAllText(fullPath);
        var seed = JsonSerializer.Deserialize<HuggingFaceCatalogSeed>(json, JsonOptions)
            ?? throw new InvalidDataException("Hugging Face catalog seed is empty.");
        Validate(seed);
        return seed;
    }

    public static void Validate(HuggingFaceCatalogSeed seed)
    {
        ArgumentNullException.ThrowIfNull(seed);
        if (seed.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported Hugging Face catalog seed schema: {seed.SchemaVersion}.");
        }

        if (seed.Slots.Count == 0)
        {
            throw new InvalidDataException("Hugging Face catalog seed has no slots.");
        }

        var duplicateSlot = seed.Slots
            .GroupBy(item => $"{item.Direction}:{item.Slot}", StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSlot is not null)
        {
            throw new InvalidDataException($"Duplicate catalog seed slot: {duplicateSlot.Key}.");
        }

        foreach (var direction in seed.Slots.GroupBy(item => item.Direction, StringComparer.OrdinalIgnoreCase))
        {
            var slots = direction.Select(item => item.Slot).Order().ToArray();
            if (slots.Length != 9 || !slots.SequenceEqual(Enumerable.Range(1, 9)))
            {
                throw new InvalidDataException($"Direction '{direction.Key}' must contain slots 1 through 9.");
            }
        }

        foreach (var slot in seed.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Direction)
                || string.IsNullOrWhiteSpace(slot.LoadLevel)
                || string.IsNullOrWhiteSpace(slot.Role)
                || string.IsNullOrWhiteSpace(slot.RepoId)
                || !slot.RepoId.Contains('/'))
            {
                throw new InvalidDataException($"Catalog seed slot {slot.Direction}:{slot.Slot} is incomplete.");
            }
        }

        if (seed.Radar.LookbackDays is < 1 or > 3650
            || seed.Radar.QueryLimit is < 1 or > 100
            || seed.Radar.MaximumNewEntriesPerSync is < 1 or > 100
            || seed.Radar.MinimumParameterCountExclusive < 0
            || seed.Radar.AutomaticTrendingRankLimit is < 1 or > 100)
        {
            throw new InvalidDataException("Hugging Face radar settings are outside safety limits.");
        }
    }
}
