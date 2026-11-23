using System.IO;
using ValveKeyValue;

namespace CsgoConfigReplace.App.Services;

public class SteamUserService : ISteamUserService
{
    private const long SteamId64Base = 76561197960265728;

    public Task<Dictionary<string, string>> GetNicknamesAsync(string steamPath, CancellationToken ct = default)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var loginUsersPath = Path.Combine(steamPath, "config", "loginusers.vdf");

        if (!File.Exists(loginUsersPath))
            return Task.FromResult(result);

        try
        {
            using var stream = File.OpenRead(loginUsersPath);
            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
            var document = serializer.Deserialize(stream, KVSerializerOptions.DefaultOptions);

            var users = document.Name == "users"
                ? document
                : document.Children.FirstOrDefault(c => c.Name == "users");
            if (users is null)
                return Task.FromResult(result);

            foreach (var user in users.Children)
            {
                ct.ThrowIfCancellationRequested();

                var steamId64Text = user.Name;
                if (!long.TryParse(steamId64Text, out var steamId64))
                    continue;

                var steamId3 = (steamId64 - SteamId64Base).ToString();
                var personaName = user["PersonaName"]?.ToString();

                if (!string.IsNullOrWhiteSpace(personaName))
                    result[steamId3] = personaName!;
            }
        }
        catch
        {
            // If parsing fails, return whatever we have; nicknames are optional.
        }

        return Task.FromResult(result);
    }
}
