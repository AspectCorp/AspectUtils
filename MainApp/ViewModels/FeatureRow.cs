using System.Windows;
using AspectUtils.Desktop.Models;
using AspectUtils.Desktop.Services;

namespace AspectUtils.Desktop.ViewModels;

public sealed class FeatureRow : ObservableObject
{
    private bool _isSelected;

    public FeatureRow(TweakDefinition definition)
    {
        Definition = definition;
    }

    public TweakDefinition Definition { get; }
    public string Name => Definition.Content;
    public string Category => AppSession.FormatCategory(Definition.DisplayCategory);
    public string? Description => Definition.Description;
    public string FeatureList => string.Join(", ", Definition.Feature ?? new List<string>());

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public sealed class FeatureActionRow : ObservableObject
{
    public FeatureActionRow(TweakDefinition definition)
    {
        Definition = definition;
    }

    public TweakDefinition Definition { get; }
    public string Name => Definition.Content;
    public string? Description => Definition.Description;
    public Visibility DescriptionVisibility =>
        string.IsNullOrWhiteSpace(Definition.Description) ? Visibility.Collapsed : Visibility.Visible;
}
