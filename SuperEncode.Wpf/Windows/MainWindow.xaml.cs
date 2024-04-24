using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace SuperEncode.Wpf.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private string _currentDirectory = Environment.CurrentDirectory;
        private readonly string _fontDirectory = "C:\\Windows\\Fonts";

        public MainWindow()
        {
            InitializeComponent();
            LoadFonts("");
            TxtFontName.Text = "UVN VAN";

        }

        private void LoadFonts(string filterName)
        {
            var installedFonts = Fonts.GetFontFamilies(_fontDirectory)
                .Where(x =>
                {
                    var fontName = x.Source.Split("#")[^1];
                    return (fontName.StartsWith("UTM") || fontName.StartsWith("UVF") || fontName.StartsWith("UVN"))
                           && fontName.Contains(filterName, StringComparison.OrdinalIgnoreCase);
                }).ToList();

            if (CmbFont != null)
            {
                CmbFont.ItemsSource = installedFonts;
                CmbFont.SelectedIndex = 0;
            }
        }

        private void BtnSelectPath_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog();
            folderDialog.ShowDialog();

            _currentDirectory = folderDialog.FolderName;

            TxtPath.Text = _currentDirectory;

            var aviFiles = Directory.GetFiles(TxtPath.Text, "*.avi", SearchOption.TopDirectoryOnly);
            var mkvFiles = Directory.GetFiles(TxtPath.Text, "*.mkv", SearchOption.TopDirectoryOnly);

            string[] files = [..aviFiles, ..mkvFiles];

            TxtFileCount.Text = files.Length.ToString();

            PbStatus.Value = 0;
            PbStatus.Maximum = files.LongLength;
        }

        private void TxtFontName_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (textBox?.Text != null) LoadFonts(textBox.Text);
        }

        private void CmbFont_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbFont.SelectedIndex < 0) return;

            var selectedFont = (FontFamily)CmbFont.SelectedItem;

            if (selectedFont != null)
            {
                var typeFaces =
                    Fonts.GetTypefaces(_fontDirectory)
                        .Where(x => x.FontFamily.Source.Equals(selectedFont.Source))
                        .Distinct();

                CmbFontType.ItemsSource = typeFaces.ToList();

            }


        }

        private void BtnDelete_OnClick(object sender, RoutedEventArgs e)
        {
            PbStatus.Value = PbStatus.Maximum = 0;
            TxtFileCount.Text = "0";
        }

        private void BtnRun_OnClick(object sender, RoutedEventArgs e)
        {
            
        }
    }
}