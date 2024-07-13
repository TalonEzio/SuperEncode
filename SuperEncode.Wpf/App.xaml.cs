using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using SuperEncode.Wpf.ViewModels;
using SuperEncode.Wpf.Windows;
using SuperEncode.Wpf.Services;
using System.Text.Json;
using SuperEncode.Wpf.Models;

namespace SuperEncode.Wpf
{
    public partial class App
    {
        readonly string _applicationPath = AppContext.BaseDirectory;

        private readonly string _applicationDataPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(SuperEncode));

        public App()
        {
            var serviceProvider = ConfigureService();

            InitializeComponent();

            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();

            try
            {
                var mainViewModel = serviceProvider.GetRequiredService<MainViewModel>();

                var settingFilePath = Path.Combine(_applicationDataPath, "SuperEncode-Config.json");

                if (!File.Exists(settingFilePath))
                {
                    File.Copy(Path.Combine(AppContext.BaseDirectory, "AssStyles", "SuperEncode-Config.Default.json"), settingFilePath);
                }

                read:
                var settingJsonContent = File.ReadAllText(settingFilePath);
                try
                {
                    if (!string.IsNullOrEmpty(settingJsonContent))
                    {
                        var settings = JsonSerializer.Deserialize<SettingJson>(settingJsonContent);

                        mainViewModel.SubtitleSetting = settings?.SubtitleSetting ?? new SubtitleSetting();
                        mainViewModel.VideoSetting = settings?.VideoSetting ?? new VideoSetting();
                    }
                }
                catch
                {
                    MessageBox.Show("Lỗi không đọc được cấu hình.\nChuyển qua đọc cấu hình mặc định", "Lỗi đọc cấu hình",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    if (File.Exists(settingFilePath))
                    {
                        File.Delete(settingFilePath);
                    }
                    File.Copy(Path.Combine(AppContext.BaseDirectory, "AssStyles", "SuperEncode-Config.Default.json"),
                        settingFilePath);
                    goto read;
                }


                mainWindow.Show();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void ScanConfig()
        {
            if (!Directory.Exists(_applicationDataPath))
            {
                Directory.CreateDirectory(_applicationDataPath);
            }

            var config = Path.Combine(_applicationDataPath, "SuperEncode-Config.json");
            if (File.Exists(config)) return;

            var defaultConfig = Path.Combine(_applicationPath, "SuperEncode-Config.Default.json");

            if (File.Exists(defaultConfig))
            {
                File.Copy(defaultConfig, config);
            }
            else
            {
                MessageBox.Show("Không đọc được cấu hình mặc định!", "Lỗi đọc cấu hình", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private IServiceProvider ConfigureService()
        {
            ScanConfig();


            IServiceCollection service = new ServiceCollection();

            service.AddScoped<MainWindow>();
            service.AddScoped<SubtitleService>();
            service.AddScoped<VideoService>();

            service.AddScoped<MainViewModel>();

            return service.BuildServiceProvider();
        }


    }

}
