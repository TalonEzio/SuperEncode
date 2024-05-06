
using System.Windows.Media;

namespace SuperEncode.Wpf.ViewModels
{
    public class SubtitleSetting : BaseViewModel
    {
        private FontFamily? _fontFamily;

        public FontFamily? FontFamily
        {
            get => _fontFamily;
            set => SetField(ref _fontFamily, value);
        }


        private bool _overrideStyleDefault;

        public bool OverrideStyleDefault
        {
            get => _overrideStyleDefault;
            set => SetField(ref _overrideStyleDefault, value);
        }

        private string _fontSearchText = string.Empty;

        public string FontSearchText
        {
            get => _fontSearchText;
            set => SetField(ref _fontSearchText, value);
        }

        private bool _bold = true;

        public bool Bold
        {
            get => _bold;
            set => SetField(ref _bold, value);
        }

        private bool _italic;

        public bool Italic
        {
            get => _italic;
            set => SetField(ref _italic, value);
        }

        private bool _underline;

        public bool Underline
        {
            get => _underline;
            set => SetField(ref _underline, value);
        }
        private bool _strikeout;

        public bool Strikeout
        {
            get => _strikeout;
            set => SetField(ref _strikeout, value);
        }

        private int _fontSize;

        public int FontSize
        {
            get => _fontSize;
            set => SetField(ref _fontSize, value);
        }

        private int _maxBitrate;

        public int MaxBitrate
        {
            get => _maxBitrate;
            set => SetField(ref _maxBitrate, value);
        }

        private double _outLine;

        public double OutLine
        {
            get => _outLine;
            set => SetField(ref _outLine, value);
        }

        private string _website = string.Empty;

        public string Website
        {
            get => _website;
            set => SetField(ref _website, value);
        }

        private string _marquee = string.Empty;

        public string Marquee
        {
            get => _marquee;
            set => SetField(ref _marquee, value);
        }

        private string _suffixSubtitle = ".vi,.vie";

        public string SuffixSubtitle
        {
            get => _suffixSubtitle;
            set => SetField(ref _suffixSubtitle, value);
        }
        private bool _subtitleInFile;
        public bool SubtitleInFile
        {
            get => _subtitleInFile;
            set => SetField(ref _subtitleInFile, value);
        }
    }
}
