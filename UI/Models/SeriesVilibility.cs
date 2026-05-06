using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace AcmeUI.Models;

public class SeriesVisibility : ObservableObject
{
    public string? Name { get; set; }

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (SetProperty(ref _isVisible, value))
                OnVisibilityChanged?.Invoke(Name, value);
        }
    }

    public event Action<string?, bool>? OnVisibilityChanged;
}
