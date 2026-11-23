using System.IO;
using ValveKeyValue;

namespace CsgoConfigReplace.App.Services;

public class CloudSyncService : ICloudSyncService
{
    private static readonly string[] ConfigPath = { "UserRoamingConfigStore", "Software", "Valve", "Steam", "apps" };

    public async Task DisableCloudSyncAsync(string userDataPath, CancellationToken ct = default)
    {
        var sharedConfigPath = GetSharedConfigPath(userDataPath);
        Directory.CreateDirectory(Path.GetDirectoryName(sharedConfigPath)!);

        KVObject document;
        if (File.Exists(sharedConfigPath))
        {
            using var stream = File.OpenRead(sharedConfigPath);
            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
            document = serializer.Deserialize(stream, KVSerializerOptions.DefaultOptions);
        }
        else
        {
            document = new KVObject("UserRoamingConfigStore", Array.Empty<KVObject>());
        }

        var newDocument = SetCloudEnabled(document, ConfigPath, 0);
        await SaveDocumentAsync(sharedConfigPath, newDocument, ct);
    }

    public Task<bool> IsCloudSyncEnabledAsync(string userDataPath, CancellationToken ct = default)
    {
        var sharedConfigPath = GetSharedConfigPath(userDataPath);
        if (!File.Exists(sharedConfigPath))
            return Task.FromResult(true);

        try
        {
            using var stream = File.OpenRead(sharedConfigPath);
            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
            var document = serializer.Deserialize(stream, KVSerializerOptions.DefaultOptions);

            var apps = NavigateToApps(document);
            var app730 = apps?.Children.FirstOrDefault(c => c.Name == "730");
            var cloudEnabled = app730?["cloudenabled"]?.ToString();

            return Task.FromResult(cloudEnabled != "0");
        }
        catch
        {
            return Task.FromResult(true);
        }
    }

    private static string GetSharedConfigPath(string userDataPath)
    {
        return Path.Combine(userDataPath, "7", "remote", "sharedconfig.vdf");
    }

    private static KVObject? NavigateToApps(KVObject document)
    {
        var current = document;
        foreach (var name in ConfigPath.Skip(1))
        {
            current = current.Children.FirstOrDefault(c => c.Name == name);
            if (current is null)
                return null;
        }
        return current;
    }

    private static KVObject SetCloudEnabled(KVObject node, string[] path, int depth)
    {
        if (depth == path.Length - 1)
        {
            // Current node is "apps"; add or replace 730.
            var children = node.Children.Where(c => c.Name != "730").ToList();
            children.Add(new KVObject("730", new[] { new KVObject("cloudenabled", "0") }));
            return new KVObject(node.Name, children);
        }

        var childName = path[depth + 1];
        var childExists = node.Children.Any(c => c.Name == childName);
        var newChildren = node.Children
            .Select(c => c.Name == childName ? SetCloudEnabled(c, path, depth + 1) : c)
            .ToList();

        if (!childExists)
        {
            newChildren.Add(CreatePath(path, depth + 1));
        }

        return new KVObject(node.Name, newChildren);
    }

    private static KVObject CreatePath(string[] path, int depth)
    {
        if (depth == path.Length - 1)
        {
            return new KVObject(path[depth], new[]
            {
                new KVObject("730", new[] { new KVObject("cloudenabled", "0") })
            });
        }

        return new KVObject(path[depth], new[] { CreatePath(path, depth + 1) });
    }

    private static Task SaveDocumentAsync(string path, KVObject document, CancellationToken ct)
    {
        var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
        using var stream = new MemoryStream();
        serializer.Serialize(stream, document);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        File.WriteAllText(path, content);
        return Task.CompletedTask;
    }
}
