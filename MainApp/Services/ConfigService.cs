using System.IO;
using System.Text;
using System.Text.Json;
using AspectUtils.Desktop.Models;

namespace AspectUtils.Desktop.Services;

public sealed class ConfigService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private ConfigService(
        string configDirectory,
        IReadOnlyDictionary<string, AppDefinition> apps,
        IReadOnlyDictionary<string, TweakDefinition> tweaks,
        IReadOnlyDictionary<string, TweakDefinition> features,
        IReadOnlyDictionary<string, List<string>> presets)
    {
        ConfigDirectory = configDirectory;
        Apps = apps;
        Tweaks = tweaks;
        Features = features;
        Presets = presets;
    }

    public string ConfigDirectory { get; }
    public IReadOnlyDictionary<string, AppDefinition> Apps { get; }
    public IReadOnlyDictionary<string, TweakDefinition> Tweaks { get; }
    public IReadOnlyDictionary<string, TweakDefinition> Features { get; }
    public IReadOnlyDictionary<string, List<string>> Presets { get; }

    public static ConfigService Load()
    {
        var directory = FindConfigDirectory();

        if (!File.Exists(Path.Combine(directory, "tweaks.json")))
        {
            throw new DirectoryNotFoundException(
                $"Could not find the configuration files. Expected them in '{directory}'.");
        }

        var apps = ReadDictionary<AppDefinition>(Path.Combine(directory, "applications.json"));
        var tweaks = ReadDictionary<TweakDefinition>(Path.Combine(directory, "tweaks.json"));
        var features = ReadDictionary<TweakDefinition>(Path.Combine(directory, "feature.json"));
        var presets = ReadDictionary<List<string>>(Path.Combine(directory, "preset.json"));

        foreach (var (key, tweak) in tweaks)
        {
            tweak.Key = key;
        }

        foreach (var (key, feature) in features)
        {
            feature.Key = key;
        }

        return new ConfigService(directory, apps, tweaks, features, presets);
    }

    public IReadOnlyList<string> ApplicationCategories =>
        Apps.Values.Select(app => app.Category)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public IReadOnlyList<string> TweakCategories =>
        Tweaks.Values.Select(tweak => tweak.DisplayCategory)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public IReadOnlyList<TweakDefinition> GetPresetTweaks(string preset)
    {
        if (!Presets.TryGetValue(preset, out var keys))
        {
            return Array.Empty<TweakDefinition>();
        }

        return keys
            .Where(key => Tweaks.ContainsKey(key))
            .Select(key => Tweaks[key])
            .ToList();
    }

    private static string FindConfigDirectory()
    {
        var candidates = new List<string> { Path.Combine(AppContext.BaseDirectory, "config") };

        // Support running from bin/Debug, bin/Release, and the repository root
        // after the source tree was split into MainApp/ and PowerShellApp/.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; directory is not null && depth < 8; depth++, directory = directory.Parent)
        {
            candidates.Add(Path.Combine(directory.FullName, "PowerShellApp", "config"));
            candidates.Add(Path.Combine(directory.FullName, "config"));
        }

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(Path.Combine(candidate, "tweaks.json")))
            {
                return candidate;
            }
        }

        return candidates[0];
    }

    private static Dictionary<string, T> ReadDictionary<T>(string path)
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        }

        var json = File.ReadAllText(path);
        var loaded = JsonSerializer.Deserialize<Dictionary<string, T>>(NormalizeJson(json), Options);
        return loaded is null
            ? new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, T>(loaded, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The shared configuration keeps multi-line scripts as literal newlines inside JSON strings.
    /// PowerShell's ConvertFrom-Json accepts that, but strict JSON does not, so escape raw control
    /// characters while leaving the files themselves untouched.
    /// </summary>
    private static string NormalizeJson(string json)
    {
        var builder = new StringBuilder(json.Length + 128);
        var inString = false;
        var escaped = false;

        foreach (var character in json)
        {
            if (inString)
            {
                if (escaped)
                {
                    builder.Append(character);
                    escaped = false;
                    continue;
                }

                switch (character)
                {
                    case '\\':
                        builder.Append(character);
                        escaped = true;
                        continue;
                    case '"':
                        builder.Append(character);
                        inString = false;
                        continue;
                    case '\r':
                        builder.Append("\\r");
                        continue;
                    case '\n':
                        builder.Append("\\n");
                        continue;
                    case '\t':
                        builder.Append("\\t");
                        continue;
                    default:
                        if (character < ' ')
                        {
                            builder.Append($"\\u{(int)character:x4}");
                            continue;
                        }

                        builder.Append(character);
                        continue;
                }
            }

            if (character == '"')
            {
                inString = true;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}
