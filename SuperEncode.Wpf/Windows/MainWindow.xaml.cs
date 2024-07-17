using System.Windows.Input;
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

        //private void MainWindow_OnMouseMove(object sender, MouseEventArgs e)
        //{
        //    if (e is { LeftButton: MouseButtonState.Pressed })
        //    {
        //        Dispatcher.Invoke(() => Window.DragMove());
        //    }
        //}
    }
}