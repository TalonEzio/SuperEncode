using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Windows;
using SuperEncode.Wpf.Extensions;
using SuperEncode.Wpf.ViewModels;

namespace SuperEncode.Wpf.Windows
{
    public partial class MainWindow
    {
        readonly string _applicationPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        private readonly MainViewModel _mainViewModel;
        public MainWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();


            _mainViewModel = mainViewModel;
            
            DataContext = _mainViewModel;
            
            Loaded += MainWindow_Loaded;

        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _mainViewModel.LoadAsync();
        }
        
        private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            var fontSearchText = _mainViewModel.SubtitleSetting.GetFontName();

            if (fontSearchText.Contains("Bold")) fontSearchText = fontSearchText.Replace("Bold", "").TrimEnd();


            var jsonObject = new
            {
                Settings = new
                {
                    _mainViewModel.VideoSetting,
                    SubtitleSetting = new
                    {
                        _mainViewModel.SubtitleSetting.Website,
                        _mainViewModel.SubtitleSetting.MaxBitrate,
                        _mainViewModel.SubtitleSetting.OutLine,
                        _mainViewModel.SubtitleSetting.FontSize,
                        _mainViewModel.SubtitleSetting.Bold,
                        _mainViewModel.SubtitleSetting.Italic,
                        _mainViewModel.SubtitleSetting.Underline,
                        _mainViewModel.SubtitleSetting.Strikeout,
                        FontSearchText = fontSearchText,
                        _mainViewModel.SubtitleSetting.Marquee,
                        _mainViewModel.SubtitleSetting.OverrideSubtitle,

                    }
                }
            };

            var json = JsonSerializer.Serialize(jsonObject, JsonSerializerOptions.Default);

            var configPath = Path.Combine(_applicationPath, "Config.json");

            if (!File.Exists(configPath)) File.Create(configPath);

            using var fileStream = new FileStream(configPath, FileMode.Truncate);
            var writerStream = new StreamWriter(fileStream, Encoding.Unicode);
            writerStream.WriteAsync(json);

            writerStream.Close();
            fileStream.Close();
        }
    }
}