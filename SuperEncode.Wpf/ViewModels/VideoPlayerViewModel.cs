using CommunityToolkit.Mvvm.ComponentModel;

namespace SuperEncode.Wpf.ViewModels;

public partial class VideoPlayerViewModel : ObservableObject
{
    [ObservableProperty] private long _currentTime;

    [ObservableProperty] private long _totalTime;
}