using System.IO;
using System.Windows;
using LibVLCSharp.Shared;

namespace SuperEncode.Wpf.UserControls
{
    public partial class UcVideoPlayer
    {
        private readonly LibVLC _libVlc;
        private readonly MediaPlayer _mediaPlayer;
        public UcVideoPlayer()
        {
            InitializeComponent();
            _libVlc = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVlc);

            VideoView.MediaPlayer = _mediaPlayer;
        }
        public static readonly DependencyProperty VideoUrlProperty =
            DependencyProperty.Register(
                nameof(VideoUrl), typeof(string), typeof(UcVideoPlayer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty SubtitleUrlProperty =
            DependencyProperty.Register(
                nameof(SubtitleUrl), typeof(string), typeof(UcVideoPlayer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string VideoUrl
        {
            get => (string)GetValue(VideoUrlProperty);

            set
            {
                SetValue(VideoUrlProperty, value);

                if (!File.Exists(VideoUrl)) return;

                var media = new Media(_libVlc, new Uri(VideoUrl));

                _mediaPlayer.Media = media;

                media.AddOption($":sub-file={SubtitleUrl}");
                _mediaPlayer.Play();

                _mediaPlayer.Time += 150000;
            }
        }

        public string SubtitleUrl
        {
            get => (string)GetValue(SubtitleUrlProperty);
            set => SetValue(SubtitleUrlProperty, value);
        }
    }
}
