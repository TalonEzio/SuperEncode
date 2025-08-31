using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Notification.Wpf;
using SuperEncode.Wpf.Models;
using SuperEncode.Wpf.Services;
using SuperEncode.Wpf.ViewModels;
using SuperEncode.Wpf.Windows;

namespace SuperEncode.Wpf;

public partial class App
{
    private readonly string _applicationDataPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(SuperEncode));

    private readonly string _applicationPath = AppContext.BaseDirectory;

    public App()
    {
        InitializeComponent();
    }

    public static List<Process> RunningProcesses { get; set; } = [];

    protected override void OnStartup(StartupEventArgs e)
    {
        var serviceProvider = ConfigureService();

        var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        try
        {
            var mainViewModel = serviceProvider.GetRequiredService<MainViewModel>();

            var settingFilePath = Path.Combine(_applicationDataPath, "SuperEncode-Config.json");

            if (!File.Exists(settingFilePath))
                File.Copy(Path.Combine(AppContext.BaseDirectory, "AssStyles", "SuperEncode-Config.Default.json"),
                    settingFilePath);

            readJsonConfig:
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

                if (File.Exists(settingFilePath)) File.Delete(settingFilePath);
                File.Copy(Path.Combine(AppContext.BaseDirectory, "AssStyles", "SuperEncode-Config.Default.json"),
                    settingFilePath);
                goto readJsonConfig;
            }

            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        foreach (var runningProcess in RunningProcesses) runningProcess.Kill();
    }

    private void ScanConfig()
    {
        if (!Directory.Exists(_applicationDataPath)) Directory.CreateDirectory(_applicationDataPath);

        var config = Path.Combine(_applicationDataPath, "SuperEncode-Config.json");
        if (File.Exists(config)) return;

        var defaultConfig = Path.Combine(_applicationPath, "SuperEncode-Config.Default.json");

        if (File.Exists(defaultConfig))
            File.Copy(defaultConfig, config);
        else
            MessageBox.Show("Không đọc được cấu hình mặc định!", "Lỗi đọc cấu hình", MessageBoxButton.OK,
                MessageBoxImage.Error);
    }

    private IServiceProvider ConfigureService()
    {
        ScanConfig();

        IServiceCollection services = new ServiceCollection();

        services.AddSingleton<INotificationManager, NotificationManager>();

        services.AddScoped<MainWindow>();
        services.AddScoped<SubtitleService>();
        services.AddScoped<VideoService>();

        services.AddScoped<MainViewModel>();

        return services.BuildServiceProvider();
    }
}