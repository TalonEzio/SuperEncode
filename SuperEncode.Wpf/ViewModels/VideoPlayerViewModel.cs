using CommunityToolkit.Mvvm.ComponentModel;

namespace SuperEncode.Wpf.ViewModels
{
    public partial class VideoPlayerViewModel : ObservableObject
    {
        [ObservableProperty]
        private long _currentTime = 0;
        [ObservableProperty]
        private long _totalTime = 0;
    }
}
