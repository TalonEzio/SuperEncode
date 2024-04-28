using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using SuperEncode.Wpf.ViewModels;
using SuperEncode.Wpf.Windows;
using System.Reflection;
using SuperEncode.Wpf.Services;

namespace SuperEncode.Wpf
{
    public partial class App
    {
        readonly string _applicationPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        public App()
        {
            Xabe.FFmpeg.FFmpeg.SetExecutablesPath(Path.Combine(_applicationPath,"Tools"));

            InitializeComponent();

            ScanConfig();
            var serviceProvider = ConfigureService();

            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();

            try
            {
                mainWindow.Show();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void ScanConfig()
        {
            var config = Path.Combine(_applicationPath, "Config.json");
            if (File.Exists(config)) return;


            var defaultConfig = Path.Combine(_applicationPath, "Config.Default.json");

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

            var configuration = new ConfigurationBuilder()
                .SetBasePath(_applicationPath)
                .AddJsonFile("Config.json")
                .Build();

            IServiceCollection service = new ServiceCollection();

            service.AddScoped<MainWindow>();
            service.AddScoped<SubtitleService>();
            service.AddScoped<VideoService>();

            service.AddScoped(_ =>
            {
                    var mainViewModel = new MainViewModel(new VideoService(new SubtitleService()));
                configuration.Bind("Settings",mainViewModel);
                return mainViewModel;
            });
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/logs.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            service.AddLogging(x =>
            {
                x.ClearProviders();
            });

            service.AddSingleton(Log.Logger);

            return service.BuildServiceProvider();
        }


    }

}
