using CommunityToolkit.Mvvm.ComponentModel;

namespace SuperEncode.Wpf.ViewModels
{
    public partial class EncodeVideoInput :ObservableObject
    {
        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private double _percent;
    }
}
