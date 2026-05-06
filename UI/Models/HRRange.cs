using CommunityToolkit.Mvvm.ComponentModel;

namespace AcmeUI.Models;

public class HRRange : ObservableObject
{
    private float _start;
    public float Start
    {
        get => _start;
        set => SetProperty(ref _start, value);
    }

    private float _end;
    public float End
    {
        get => _end;
        set => SetProperty(ref _end, value);
    }
}

