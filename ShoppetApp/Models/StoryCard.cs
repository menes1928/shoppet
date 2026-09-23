using CommunityToolkit.Mvvm.ComponentModel;

namespace ShoppetApp.Models;

public partial class StoryCard : ObservableObject
{
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;

    [ObservableProperty] private double _rotation;
    [ObservableProperty] private double _translationY;
    [ObservableProperty] private double _translationX;
    [ObservableProperty] private double _scale = 1;
    [ObservableProperty] private double _opacity = 1;
    [ObservableProperty] private int _zIndex;
    [ObservableProperty] private bool _isActive;
}
