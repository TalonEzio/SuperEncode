using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Microsoft.Extensions.Logging;
using Serilog;
using SuperEncode.Wpf.Windows;

namespace SuperEncode.Wpf
{
    public partial class App
    {
        public App()
        {
            InitializeComponent();

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
        private static IServiceProvider ConfigureService()
        {
            IServiceCollection service = new ServiceCollection();
            service.AddScoped<MainWindow>();
            
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
