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