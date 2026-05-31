using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class UserProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public UserProfile LoadOrCreate()
    {
        AppDataPaths.EnsureBaseDirectory();
        if (!File.Exists(AppDataPaths.UserProfilePath))
        {
            var profile = new UserProfile();
            Save(profile);
            return profile;
        }

        try
        {
            var json = File.ReadAllText(AppDataPaths.UserProfilePath);
            var profile = JsonSerializer.Deserialize<UserProfile>(json, JsonOptions) ?? new UserProfile();
            Save(profile);
            return profile;
        }
        catch
        {
            return new UserProfile();
        }
    }

    public void Save(UserProfile profile)
    {
        AppDataPaths.EnsureBaseDirectory();
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(AppDataPaths.UserProfilePath, json);
    }
}
