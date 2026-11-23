using System.IO;
using System.Text.Json;
using CsgoConfigReplace.App.Models;

namespace CsgoConfigReplace.App.Services;

public class SettingsService : ISettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CsgoConfigReplace");

    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(SettingsPath))
            return Task.FromResult(new AppSettings());

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return Task.FromResult(JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings());
        }
        catch
        {
            return Task.FromResult(new AppSettings());
        }
    }

    public Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        Directory.CreateDirectory(SettingsDirectory);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
        return Task.CompletedTask;
    }
}
