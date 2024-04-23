using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace SuperEncode.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _currentDirectory = Environment.CurrentDirectory;
        public MainWindow()
        {
            InitializeComponent();
            LoadFonts();

        }
        private void LoadFonts()
        {
            var installedFonts = Fonts.GetFontFamilies("C:\\Windows\\Fonts")
                .Where(x =>
                {
                    var fontName = x.Source.Split("#")[^1];
                    return fontName.StartsWith("UTM") || fontName.StartsWith("UVF") || fontName.StartsWith("UVN");
                });

            CmbFont.ItemsSource = installedFonts;
        }
        private void BtnSelectPath_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog();
            folderDialog.ShowDialog();

            _currentDirectory = folderDialog.FolderName;

            TxtPath.Text = _currentDirectory;
        }
    }
}