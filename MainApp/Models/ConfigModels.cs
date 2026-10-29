using System.Text.Json;
using System.Text.Json.Serialization;

namespace AspectUtils.Desktop.Models;

public sealed class AppDefinition
{
    public string Category { get; set; } = "Uncategorized";
    public string? Choco { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Link { get; set; }
    public string? Winget { get; set; }
    public bool Foss { get; set; }

    public string PackageId => !string.IsNullOrWhiteSpace(Winget) ? Winget! : Choco ?? string.Empty;
}

public sealed class TweakDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Panel { get; set; }

    public List<RegistryChange>? Registry { get; set; }
    public List<ServiceChange>? Service { get; set; }
    public List<string>? InvokeScript { get; set; }
    public List<string>? UndoScript { get; set; }
    public List<string>? Feature { get; set; }
    public List<string>? Appx { get; set; }

    public string? Type { get; set; }
    public string? Function { get; set; }
    public string? Link { get; set; }
    public string? ButtonWidth { get; set; }

    public string DisplayCategory => string.IsNullOrWhiteSpace(Category) ? "Other" : Category!;
}

public sealed class RegistryChange
{
    public string Path { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Type { get; set; }
    public JsonElement? Value { get; set; }
    public JsonElement? OriginalValue { get; set; }
    public JsonElement? DefaultValue { get; set; }
    public Dictionary<string, JsonElement>? Values { get; set; }

    public bool IsStateSelector => Values is { Count: > 0 } && Value is null;
}

public sealed class ServiceChange
{
    public string Name { get; set; } = string.Empty;
    public string? StartupType { get; set; }
    public string? OriginalType { get; set; }
}

public sealed class RegistrySnapshot
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Existed { get; set; }
    public int Kind { get; set; }
    public string? EncodedValue { get; set; }
}

public sealed class ServiceSnapshot
{
    public string Name { get; set; } = string.Empty;
    public string? StartupType { get; set; }
}

public sealed class FeatureSnapshot
{
    public string Name { get; set; } = string.Empty;
    public string? State { get; set; }
}

public sealed class AppliedTweakRecord
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset AppliedAt { get; set; } = DateTimeOffset.Now;
    public bool AppliedByDesktop { get; set; } = true;
    public List<RegistrySnapshot> Registry { get; set; } = new();
    public List<ServiceSnapshot> Service { get; set; } = new();
    public List<FeatureSnapshot> Features { get; set; } = new();
}

public sealed class FixAction
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ScriptFile { get; init; } = string.Empty;
    public bool RequiresConfirmation { get; init; }
    public string ConfirmationText { get; init; } = string.Empty;
}

public sealed class UpdateProfile
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ScriptFile { get; init; } = string.Empty;
    public bool RequiresConfirmation { get; init; }
    public string ConfirmationText { get; init; } = string.Empty;
    public bool Recommended { get; init; }
}

public static class JsonValue
{
    public static string? ToText(JsonElement? element)
    {
        if (element is null || element.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        var value = element.Value;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.True => "1",
            JsonValueKind.False => "0",
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.Array => string.Join(",", value.EnumerateArray().Select(item => item.ToString())),
            _ => value.ToString()
        };
    }

    public static int ToInt(JsonElement? element, int fallback = 0)
    {
        var text = ToText(element);
        return int.TryParse(text, out var value) ? value : fallback;
    }
}
