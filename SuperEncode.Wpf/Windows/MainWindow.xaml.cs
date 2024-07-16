using System.IO;
using System.Windows;
using SuperEncode.Wpf.UserControls;
using SuperEncode.Wpf.ViewModels;

namespace SuperEncode.Wpf.Windows
{
    public partial class MainWindow
    {
        public MainWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = mainViewModel;
        }

    }
}