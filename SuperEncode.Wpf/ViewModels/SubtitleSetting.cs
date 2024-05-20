using System.Text.Json.Serialization;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuperEncode.Wpf.ViewModels
{
    public partial class SubtitleSetting : ObservableObject
    {
        [ObservableProperty]
        [property: JsonIgnore] //Apply [JsonIgnore] for FontFamily Property
        private FontFamily? _fontFamily;

        [ObservableProperty]
        private bool _overrideStyleDefault;



        [ObservableProperty]
        private string? _fontSearchText;


        [ObservableProperty]
        private bool _bold;


        [ObservableProperty]
        private bool _italic;

        [ObservableProperty]
        private bool _underline;


        [ObservableProperty]
        private bool _strikeout;

        [ObservableProperty]
        private int _fontSize;


        [ObservableProperty]
        private int _maxBitrate;

        [ObservableProperty]
        private double _outLine;
        [ObservableProperty]
        private string _website = string.Empty;

        [ObservableProperty]
        private string? _marquee;


        [ObservableProperty]
        private string _suffixSubtitle = ".vi,.vie";

        [ObservableProperty]
        private bool _subtitleInFile;

    }
}
