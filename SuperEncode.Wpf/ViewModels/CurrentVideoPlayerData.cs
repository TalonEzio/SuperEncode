using CommunityToolkit.Mvvm.ComponentModel;

namespace SuperEncode.Wpf.ViewModels
{
    public partial class CurrentVideoPlayerData : ObservableObject
    {
        [ObservableProperty]
        private string _videoUrl = string.Empty;

        [ObservableProperty]
        private string _subtitleUrl = string.Empty;
    }
}
