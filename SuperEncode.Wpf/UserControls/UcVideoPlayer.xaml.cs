using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using LibVLCSharp.Shared;
using Color = System.Drawing.Color;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace SuperEncode.Wpf.UserControls
{
    public partial class UcVideoPlayer 
    {

        private readonly LibVLC _libVlc;
        private readonly MediaPlayer _mediaPlayer;

        

        public UcVideoPlayer()
        {
            _libVlc = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVlc);
            InitializeComponent();
            VideoView.MediaPlayer = _mediaPlayer;


        }
        public static readonly DependencyProperty VideoUrlProperty =
            DependencyProperty.Register(
               nameof(VideoUrl), typeof(string), typeof(UcVideoPlayer));

        public static void OnSubTitleVideoUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var newSubtitleUrl = (string)e.NewValue;
            var ucVideoPlayer = (UcVideoPlayer)d;

            if (string.IsNullOrEmpty(newSubtitleUrl))
            {
                if (ucVideoPlayer._mediaPlayer.IsPlaying)
                {
                    ucVideoPlayer._mediaPlayer.Stop();
                }
                return;
            }
            ucVideoPlayer.VideoView.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));


            ucVideoPlayer.LoadMedia(subtitleUrl: newSubtitleUrl);
        }

        private async void LoadMedia(string subtitleUrl)
        {
            if (!File.Exists(subtitleUrl))
            {
                return;
            }

            var media = new Media(_libVlc, new Uri(VideoUrl));
            _mediaPlayer.Media = media;
            media.AddOption($":sub-file={subtitleUrl}");

           

            _mediaPlayer.Play();

            await Task.Delay(1000);
            Slider.Minimum = 0;
            Slider.Value = 0;
            Slider.Maximum = _mediaPlayer.Media.Duration;
        }

        public static readonly DependencyProperty SubtitleUrlProperty =
            DependencyProperty.Register(
                nameof(SubtitleUrl), typeof(string), typeof(UcVideoPlayer), new FrameworkPropertyMetadata(OnSubTitleVideoUrlChanged));

        public string VideoUrl
        {
            get => (string)GetValue(VideoUrlProperty);

            set => SetValue(VideoUrlProperty, value);
        }

        public string SubtitleUrl
        {
            get => (string)GetValue(SubtitleUrlProperty);
            set => SetValue(SubtitleUrlProperty, value);
        }

        private void BtnPause_OnClick(object sender, RoutedEventArgs e)
        {
            var mediaPlayer = VideoView.MediaPlayer;
            if (mediaPlayer == null) return;
            if (mediaPlayer.CanPause)
            {
                mediaPlayer.Pause();
            }
        }

        private void BtnPrev_OnClick(object sender, RoutedEventArgs e)
        {
            var mediaPlayer = VideoView.MediaPlayer;

            if (mediaPlayer is { Time: >= 30000 })
            {
                mediaPlayer.Time -= 30000;
                Slider.Value -= 30000;
            }

        }

        private void BtnNext_OnClick(object sender, RoutedEventArgs e)
        {
            var mediaPlayer = VideoView.MediaPlayer;

            if (mediaPlayer is not { Time: >= 0 }) return;

            var endTime = mediaPlayer.Length;

            if (mediaPlayer.Time + 30000 < endTime)
            {
                mediaPlayer.Time += 30000;
                Slider.Value += 30000;
            }
        }

        private void BtnStop_OnClick(object sender, RoutedEventArgs e)
        {
            VideoView.MediaPlayer?.Stop();
            Slider.Minimum =Slider.Maximum = 0;
            VideoView.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));

        }

        private void BtnContinue_OnClick(object sender, RoutedEventArgs e)
        {
            var mediaPlayer = VideoView.MediaPlayer;
            mediaPlayer?.Play();

            Slider.Minimum = 0;
            Slider.Value = _mediaPlayer.Time;
            Slider.Maximum = _mediaPlayer.Media!.Duration;
        }

        private void RangeBase_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            var mediaPlayer = VideoView.MediaPlayer;
            if (mediaPlayer != null)
            {
                mediaPlayer.Time = (long)e.NewValue;
            }
        }


    }
}
