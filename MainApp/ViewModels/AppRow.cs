using AspectUtils.Desktop.Models;

namespace AspectUtils.Desktop.ViewModels;

public sealed class AppRow : ObservableObject
{
    private bool _isSelected;

    public AppRow(AppDefinition definition)
    {
        Definition = definition;
    }

    public AppDefinition Definition { get; }
    public string Name => Definition.Content;
    public string Category => Definition.Category;
    public string? Description => Definition.Description;
    public string PackageId => Definition.PackageId;

    public string SourceText => !string.IsNullOrWhiteSpace(Definition.Winget)
        ? Definition.Choco is null ? "WinGet" : "WinGet + Chocolatey"
        : "Chocolatey";

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
