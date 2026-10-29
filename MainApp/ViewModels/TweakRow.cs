using System.Windows;
using AspectUtils.Desktop.Models;
using AspectUtils.Desktop.Services;

namespace AspectUtils.Desktop.ViewModels;

public sealed class TweakRow : ObservableObject
{
    private bool _isSelected;
    private bool _isApplied;
    private bool _canUndo;

    public TweakRow(TweakDefinition definition)
    {
        Definition = definition;
    }

    public TweakDefinition Definition { get; }
    public string Name => Definition.Content;
    public string Category => AppSession.FormatCategory(Definition.DisplayCategory);
    public string? Description => Definition.Description;

    public string SupportText
    {
        get
        {
            var parts = new List<string>();
            if (Definition.Registry is { Count: > 0 })
            {
                parts.Add(Definition.Registry.Count == 1 ? "registry value" : $"{Definition.Registry.Count} registry values");
            }

            if (Definition.Service is { Count: > 0 })
            {
                parts.Add(Definition.Service.Count == 1 ? "service" : $"{Definition.Service.Count} services");
            }

            if (Definition.Feature is { Count: > 0 })
            {
                parts.Add("Windows features");
            }

            if (Definition.InvokeScript?.Any(script => !string.IsNullOrWhiteSpace(script)) == true)
            {
                parts.Add("actions");
            }

            return string.Join(" · ", parts);
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsApplied
    {
        get => _isApplied;
        set
        {
            if (SetProperty(ref _isApplied, value))
            {
                OnPropertyChanged(nameof(AppliedVisibility));
            }
        }
    }

    public bool CanUndo
    {
        get => _canUndo;
        set
        {
            if (SetProperty(ref _canUndo, value))
            {
                OnPropertyChanged(nameof(UndoVisibility));
            }
        }
    }

    public Visibility AppliedVisibility => IsApplied ? Visibility.Visible : Visibility.Collapsed;
    public Visibility UndoVisibility => CanUndo ? Visibility.Visible : Visibility.Collapsed;
}

public sealed class TweakActionRow
{
    public TweakActionRow(TweakDefinition definition)
    {
        Definition = definition;
    }

    public TweakDefinition Definition { get; }
    public string Name => Definition.Content;
    public string? Description => Definition.Description;
    public string Category => AppSession.FormatCategory(Definition.DisplayCategory);
}
