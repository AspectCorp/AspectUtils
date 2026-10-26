using System.Globalization;
using System.Text.Json;
using AspectUtils.Desktop.Models;
using Microsoft.Win32;

namespace AspectUtils.Desktop.Services;

public sealed class RegistryService
{
    private readonly LogService _log;

    public RegistryService(LogService log)
    {
        _log = log;
    }

    public void Apply(RegistryChange change, List<RegistrySnapshot> snapshots, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (change.IsStateSelector)
        {
            _log.Warn($"Skipped registry state selector '{change.Path}\\{change.Name}' in the desktop app. Use the PowerShell app for multi-state entries.");
            return;
        }

        var valueText = JsonValue.ToText(change.Value) ?? JsonValue.ToText(change.DefaultValue);
        if (valueText is null)
        {
            _log.Warn($"Skipped registry entry '{change.Path}\\{change.Name}' because it has no value.");
            return;
        }

        var (hive, subKey) = ParsePath(change.Path);
        var name = change.Name ?? string.Empty;
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);

        if (!snapshots.Any(snapshot =>
                string.Equals(snapshot.Path, change.Path, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(snapshot.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            snapshots.Add(CaptureSnapshot(baseKey, change.Path, subKey, name));
        }

        if (string.Equals(valueText, "<RemoveEntry>", StringComparison.OrdinalIgnoreCase))
        {
            using var existingKey = baseKey.OpenSubKey(subKey, writable: true);
            if (existingKey is not null && ValueExists(existingKey, name))
            {
                existingKey.DeleteValue(name, throwOnMissingValue: false);
                _log.Info($"Removed registry value {change.Path}\\{name}");
            }

            return;
        }

        using var key = baseKey.CreateSubKey(subKey, writable: true)
                        ?? throw new InvalidOperationException($"Could not open registry key '{change.Path}'.");

        var kind = ParseKind(change.Type);
        var value = ConvertValue(valueText, kind);
        key.SetValue(name, value, kind);
        _log.Info($"Set {change.Path}\\{name} = {valueText} ({kind})");
    }

    public void Restore(RegistrySnapshot snapshot)
    {
        var (hive, subKey) = ParsePath(snapshot.Path);
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);

        if (!snapshot.Existed)
        {
            using var existingKey = baseKey.OpenSubKey(subKey, writable: true);
            if (existingKey is not null && ValueExists(existingKey, snapshot.Name))
            {
                existingKey.DeleteValue(snapshot.Name, throwOnMissingValue: false);
                _log.Info($"Undo: removed registry value {snapshot.Path}\\{snapshot.Name}");
            }

            return;
        }

        using var key = baseKey.CreateSubKey(subKey, writable: true)
                        ?? throw new InvalidOperationException($"Could not open registry key '{snapshot.Path}' for undo.");
        var kind = (RegistryValueKind)snapshot.Kind;
        var value = DecodeValue(snapshot.EncodedValue, kind) ?? string.Empty;
        key.SetValue(snapshot.Name, value, kind);
        _log.Info($"Undo: restored registry value {snapshot.Path}\\{snapshot.Name}");
    }

    public void RestoreOriginal(RegistryChange change)
    {
        var original = JsonValue.ToText(change.OriginalValue);
        var (hive, subKey) = ParsePath(change.Path);
        var name = change.Name ?? string.Empty;
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);

        if (original is null)
        {
            _log.Warn($"No original value recorded for {change.Path}\\{name}.");
            return;
        }

        if (string.Equals(original, "<RemoveEntry>", StringComparison.OrdinalIgnoreCase))
        {
            using var existingKey = baseKey.OpenSubKey(subKey, writable: true);
            if (existingKey is not null && ValueExists(existingKey, name))
            {
                existingKey.DeleteValue(name, throwOnMissingValue: false);
                _log.Info($"Undo: removed registry value {change.Path}\\{name}");
            }

            return;
        }

        using var key = baseKey.CreateSubKey(subKey, writable: true)
                        ?? throw new InvalidOperationException($"Could not open registry key '{change.Path}' for undo.");
        var kind = ParseKind(change.Type);
        key.SetValue(name, ConvertValue(original, kind), kind);
        _log.Info($"Undo: restored {change.Path}\\{name} = {original}");
    }

    private RegistrySnapshot CaptureSnapshot(
        RegistryKey baseKey,
        string originalPath,
        string subKey,
        string name)
    {
        using var key = baseKey.OpenSubKey(subKey, writable: false);
        if (key is null || !ValueExists(key, name))
        {
            return new RegistrySnapshot
            {
                Path = originalPath,
                Name = name,
                Existed = false
            };
        }

        var kind = key.GetValueKind(name);
        var value = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        return new RegistrySnapshot
        {
            Path = originalPath,
            Name = name,
            Existed = true,
            Kind = (int)kind,
            EncodedValue = EncodeValue(value, kind)
        };
    }

    private static bool ValueExists(RegistryKey key, string name) =>
        key.GetValueNames().Any(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase));

    private static RegistryHive ParseHive(string path)
    {
        var prefix = path.Split(':', 2)[0].Trim().ToUpperInvariant();
        return prefix switch
        {
            "HKLM" or "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
            "HKCU" or "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
            "HKU" or "HKEY_USERS" => RegistryHive.Users,
            "HKCR" or "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
            "HKCC" or "HKEY_CURRENT_CONFIG" => RegistryHive.CurrentConfig,
            _ => throw new ArgumentException($"Unsupported registry hive in '{path}'.")
        };
    }

    private static (RegistryHive Hive, string SubKey) ParsePath(string path)
    {
        var separator = path.IndexOf(':');
        if (separator < 0)
        {
            throw new ArgumentException($"Registry path '{path}' is missing a hive prefix.");
        }

        var subKey = path[(separator + 1)..].TrimStart('\\');
        return (ParseHive(path), subKey);
    }

    private static RegistryValueKind ParseKind(string? type) =>
        (type ?? "String").Trim().ToLowerInvariant() switch
        {
            "dword" => RegistryValueKind.DWord,
            "qword" => RegistryValueKind.QWord,
            "string" => RegistryValueKind.String,
            "expandstring" => RegistryValueKind.ExpandString,
            "binary" => RegistryValueKind.Binary,
            "multistring" => RegistryValueKind.MultiString,
            "none" => RegistryValueKind.None,
            _ => RegistryValueKind.String
        };

    private static object ConvertValue(string text, RegistryValueKind kind) =>
        kind switch
        {
            RegistryValueKind.DWord => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dword)
                ? dword
                : uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unsignedDword)
                    ? unchecked((int)unsignedDword)
                    : text.Equals("true", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            RegistryValueKind.QWord => long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var qword)
                ? qword
                : 0L,
            RegistryValueKind.Binary => Convert.FromBase64String(text),
            RegistryValueKind.MultiString => text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries),
            RegistryValueKind.None => text,
            _ => text
        };

    private static string? EncodeValue(object? value, RegistryValueKind kind) =>
        value switch
        {
            null => null,
            byte[] bytes => Convert.ToBase64String(bytes),
            string[] values => JsonSerializer.Serialize(values),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };

    private static object? DecodeValue(string? encoded, RegistryValueKind kind)
    {
        if (encoded is null)
        {
            return null;
        }

        return kind switch
        {
            RegistryValueKind.DWord => int.TryParse(encoded, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dword) ? dword : 0,
            RegistryValueKind.QWord => long.TryParse(encoded, NumberStyles.Integer, CultureInfo.InvariantCulture, out var qword) ? qword : 0L,
            RegistryValueKind.Binary => Convert.FromBase64String(encoded),
            RegistryValueKind.MultiString => JsonSerializer.Deserialize<string[]>(encoded) ?? Array.Empty<string>(),
            RegistryValueKind.None => null,
            _ => encoded
        };
    }
}
