using System.IO;
using AspectUtils.Desktop.Models;

namespace AspectUtils.Desktop.Services;

public sealed class AppSession
{
    private AppSession(
        ConfigService config,
        LogService log,
        UndoStore undo,
        TweakEngine tweaks,
        PackageManagerService packages,
        FeatureService features,
        ServiceManager serviceManager,
        string scriptsDirectory)
    {
        Config = config;
        Log = log;
        Undo = undo;
        Tweaks = tweaks;
        Packages = packages;
        Features = features;
        ServiceManager = serviceManager;
        ScriptsDirectory = scriptsDirectory;
    }

    public ConfigService Config { get; }
    public LogService Log { get; }
    public UndoStore Undo { get; }
    public TweakEngine Tweaks { get; }
    public PackageManagerService Packages { get; }
    public FeatureService Features { get; }
    public ServiceManager ServiceManager { get; }
    public string ScriptsDirectory { get; }

    public static AppSession Create()
    {
        var log = new LogService();
        var config = ConfigService.Load();
        var undo = new UndoStore(log);
        var registry = new RegistryService(log);
        var serviceManager = new ServiceManager(log);
        var features = new FeatureService(log);
        var tweaks = new TweakEngine(config, registry, serviceManager, features, undo, log);
        var packages = new PackageManagerService(log);

        var scriptsDirectory = Path.Combine(AppContext.BaseDirectory, "Scripts");
        if (!Directory.Exists(scriptsDirectory))
        {
            scriptsDirectory = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "desktop", "Scripts"));
        }

        return new AppSession(config, log, undo, tweaks, packages, features, serviceManager, scriptsDirectory);
    }

    public IReadOnlyList<TweakDefinition> ToggleTweaks =>
        Config.Tweaks.Values
            .Where(IsToggleTweak)
            .OrderBy(tweak => FormatCategory(tweak.DisplayCategory), StringComparer.OrdinalIgnoreCase)
            .ThenBy(tweak => tweak.Content, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public IReadOnlyList<TweakDefinition> ActionTweaks =>
        Config.Tweaks.Values
            .Where(IsActionTweak)
            .OrderBy(tweak => FormatCategory(tweak.DisplayCategory), StringComparer.OrdinalIgnoreCase)
            .ThenBy(tweak => tweak.Content, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public IReadOnlyList<TweakDefinition> InstallableFeatures =>
        Config.Features.Values
            .Where(feature => feature.Feature is { Count: > 0 })
            .OrderBy(feature => FormatCategory(feature.DisplayCategory), StringComparer.OrdinalIgnoreCase)
            .ThenBy(feature => feature.Content, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public IReadOnlyList<TweakDefinition> FeatureActions =>
        Config.Features.Values
            .Where(feature => feature.Feature is not { Count: > 0 } && HasScript(feature.InvokeScript))
            .OrderBy(feature => feature.Content, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static bool IsSupportedTweak(TweakDefinition tweak) =>
        !string.Equals(tweak.Type, "Button", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(tweak.Type, "Combobox", StringComparison.OrdinalIgnoreCase);

    public static bool IsActionTweak(TweakDefinition tweak) =>
        IsSupportedTweak(tweak) &&
        tweak.Registry is not { Count: > 0 } &&
        tweak.Service is not { Count: > 0 } &&
        tweak.Feature is not { Count: > 0 } &&
        tweak.Appx is not { Count: > 0 } &&
        !HasScript(tweak.UndoScript) &&
        HasScript(tweak.InvokeScript);

    public static bool IsToggleTweak(TweakDefinition tweak) =>
        IsSupportedTweak(tweak) && !IsActionTweak(tweak);

    public static bool HasScript(IEnumerable<string>? script) =>
        script?.Any(line => !string.IsNullOrWhiteSpace(line)) == true;

    public static string FormatCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return "Other";
        }

        var formatted = category.Trim();
        if (formatted.StartsWith("z__", StringComparison.OrdinalIgnoreCase))
        {
            formatted = formatted[3..];
        }

        return formatted
            .Replace(" - CAUTION", " (Caution)", StringComparison.OrdinalIgnoreCase)
            .Replace(" - NOT FOR LAPTOPS", " (Not for laptops)", StringComparison.OrdinalIgnoreCase);
    }
}
