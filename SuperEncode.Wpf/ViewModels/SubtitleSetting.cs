using System.Text.Json.Serialization;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuperEncode.Wpf.ViewModels;

public partial class SubtitleSetting : ObservableObject
{
    [ObservableProperty] private bool _bold;

    [ObservableProperty] [property: JsonIgnore] //Apply [JsonIgnore] for FontFamily Property
    private FontFamily? _fontFamily;


    [ObservableProperty] private string? _fontSearchText;

    [ObservableProperty] private int _fontSize;


    [ObservableProperty] private bool _italic;

    [ObservableProperty] private string? _marquee;


    [ObservableProperty] private int _maxBitrate;

    [ObservableProperty] private double _outLine;

    [ObservableProperty] private bool _overrideStyleDefault;


    [ObservableProperty] private bool _strikeout;

    [ObservableProperty] private bool _subtitleInFile;


    [ObservableProperty] private string _suffixSubtitle = ".vi,.vie";

    [ObservableProperty] private bool _underline;

    [ObservableProperty] private string _website = string.Empty;
}