using CommunityToolkit.Mvvm.ComponentModel;

namespace SuperEncode.Wpf.ViewModels
{

    public partial class VideoSetting : ObservableObject
    {
        [ObservableProperty]
        private bool _enableHdr;

        [ObservableProperty]
        private bool _deleteAfterEncode;

        [ObservableProperty]
        private string _inputFolder = string.Empty;

        [ObservableProperty]
        private bool _scanDeep;

        [ObservableProperty]
        private bool _fansubMode;
    }

}
