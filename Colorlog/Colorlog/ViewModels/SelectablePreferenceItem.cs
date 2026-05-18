using CommunityToolkit.Mvvm.ComponentModel;

namespace Colorlog.ViewModels;

public sealed class SelectablePreferenceItem : ObservableObject
{
    private readonly Action _onSelectionChanged;

    public SelectablePreferenceItem(string label, Action onSelectionChanged)
    {
        Label = label;
        _onSelectionChanged = onSelectionChanged;
    }

    public string Label { get; }

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                _onSelectionChanged();
            }
        }
    }
}
