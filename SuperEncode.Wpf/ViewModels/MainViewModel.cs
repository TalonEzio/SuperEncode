using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using SuperEncode.Wpf.Commands;
using SuperEncode.Wpf.Extensions;
using SuperEncode.Wpf.Services;
using Xabe.FFmpeg;
using System.Reflection;

namespace SuperEncode.Wpf.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private static readonly string ApplicationPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        private readonly string _fontDirectory = @"C:\Windows\Fonts";
        private SubtitleSetting _subtitleSetting = new();

        public SubtitleSetting SubtitleSetting
        {
            get => _subtitleSetting;
            set => SetField(ref _subtitleSetting, value);
        }
        private VideoSetting _videoSetting = new();

        public VideoSetting VideoSetting
        {
            get => _videoSetting;
            set => SetField(ref _videoSetting, value);
        }

        private bool _scanDeep;

        public bool ScanDeep
        {
            get => _scanDeep;
            set
            {
                SetField(ref _scanDeep, value);
                UpdateFileList(VideoSetting.InputFolder);
            }
        }

        private int _successCount;

        public int SuccessCount
        {
            get => _successCount;
            set => SetField(ref _successCount, value);
        }

        private bool _canRun;
        public bool CanRun
        {
            get => _canRun;
            set
            {
                SetField(ref _canRun, value);
                RunCommand.OnCanExecuteChanged();
                ResetCommand.OnCanExecuteChanged();
            }
        }
        private bool _enableWindow = true;
        public bool EnableWindow
        {
            get => _enableWindow;
            set
            {
                SetField(ref _enableWindow, value);
                RunCommand.OnCanExecuteChanged();
                ResetCommand.OnCanExecuteChanged();
            }
        }

        public ObservableCollection<string> Files { get; set; } = [];
        public ObservableCollection<FontFamily> FontFamilies { get; set; } = [];

        public RelayCommand<object?> SelectPathCommand { get; }

        public RelayCommand<string> OpenFolderCommand { get; }
        public RelayCommand<string> LoadFontCommand { get; }
        public RelayCommand<object?> ResetCommand { get; }
        public RelayCommand<object?> RunCommand { get; }
        public RelayCommand<object> ClosingFormCommand { get; }
        public RelayCommand<object> LoadedFormCommand { get; }
        public Stopwatch DurationStopwatch { get; } = new();


        private readonly VideoService _videoService;
        public MainViewModel(VideoService videoService)
        {
            _videoService = videoService;
            LoadedFormCommand = new RelayCommand<object>(LoadedForm);
            SelectPathCommand = new RelayCommand<object?>(SelectPath);
            OpenFolderCommand = new RelayCommand<string>(OpenFolder);

            LoadFontCommand = new RelayCommand<string>(UpdateFontFamilies);

            RunCommand = new RelayCommand<object?>(RunEncode, CanRunEncode);
            ResetCommand = new RelayCommand<object?>(Reset);
            ClosingFormCommand = new RelayCommand<object>(ClosingForm);
        }

        private async void LoadedForm(object obj)
        {

            await LoadAsync(); 
        }

        private void ClosingForm(object obj)
        {
            var fontSearchText = SubtitleSetting.GetFontName();

            if (fontSearchText.Contains("Bold")) fontSearchText = fontSearchText.Replace("Bold", "").TrimEnd();


            var jsonObject = new
            {
                Settings = new
                {
                    VideoSetting,
                    SubtitleSetting = new
                    {
                        SubtitleSetting.Website,
                        SubtitleSetting.MaxBitrate,
                        SubtitleSetting.OutLine,
                        SubtitleSetting.FontSize,
                        SubtitleSetting.Bold,
                        SubtitleSetting.Italic,
                        SubtitleSetting.Underline,
                        SubtitleSetting.Strikeout,
                        FontSearchText = fontSearchText,
                        SubtitleSetting.Marquee,
                        SubtitleSetting.OverrideStyleDefault,

                    }
                }
            };

            var json = JsonSerializer.Serialize(jsonObject, JsonSerializerOptions.Default);

            var configPath = Path.Combine(ApplicationPath, "Config.json");

            if (!File.Exists(configPath)) File.Create(configPath);

            using var fileStream = new FileStream(configPath, FileMode.Truncate);
            var writerStream = new StreamWriter(fileStream, Encoding.Unicode);
            writerStream.WriteAsync(json);

            writerStream.Close();
            fileStream.Close();

        }

        private bool CanRunEncode(object? arg) => CanRun;

        private async void RunEncode(object? obj)
        {
            UpdateFileList(VideoSetting.InputFolder);

            DurationStopwatch.Restart();
            EnableWindow = false;

            var timer = new Timer(_ =>
                OnPropertyChanged(nameof(DurationStopwatch)), null, 10, 10);

            foreach (var file in Files)
            {
                try
                {
                    await ProcessFile(file);
                }
                catch (FileNotFoundException)
                {
                    ShowErrorMessage($"Encode lỗi {Path.GetFileName(file)}, file không thấy!");
                }
                catch (Exception e)
                {
                    ShowErrorMessage($"Encode lỗi {Path.GetFileName(file)}!\n{e.Message}");
                }
                finally
                {
                    SuccessCount++;
                }
            }

            DurationStopwatch.Stop();

            MessageBox.Show(
                $"Xử lý hoàn tất {Files.Count} file !",
                "Thống báo",
                MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK
            );
            await timer.DisposeAsync();
            EnableWindow = true;
            SuccessCount = 0;
        }

        async Task ProcessFile(string file)
        {
            if (!File.Exists(file))
            {
                ShowErrorMessage($"Không tìm thấy file {Path.GetFileName(file)}, vui lòng xem lại!");
                return;
            }

            var fileInfo = await FFmpeg.GetMediaInfo(file);

            if (!fileInfo.SubtitleStreams.Any())
            {
                ShowErrorMessage($"File {Path.GetFileName(file)} không có phụ đề, vui lòng xem lại!");
                return;
            }

            var outputVideoFile = await _videoService.EncodeVideoWithNVencC(fileInfo, SubtitleSetting, VideoSetting);
            if (new FileInfo(outputVideoFile).Length == 0)
            {
                ShowErrorMessage($"Encode lỗi {Path.GetFileName(file)}, vui lòng xem lại!");
            }
        }

        static void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Thống báo", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
        }



        private void Reset(object? obj)
        {
            VideoSetting.InputFolder = "";

            Files.Clear();

            CanRun = Files.Any();
        }

        private void OpenFolder(string path)
        {
            if (!Directory.Exists(path)) return;
            var processInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = path
            };

            Process.Start(processInfo);
        }

        private void SelectPath(object? obj)
        {

            var folderDialog = new OpenFolderDialog();
            folderDialog.ShowDialog();

            VideoSetting.InputFolder = folderDialog.FolderName;

            UpdateFileList(VideoSetting.InputFolder);
        }

        private IEnumerable<string> ScanFiles(string path)
        {
            if (!Directory.Exists(path)) return [];

            var searchOption = ScanDeep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var aviFiles = Directory.GetFiles(path, "*.avi", searchOption);
            var mkvFiles = Directory.GetFiles(path, "*.mkv", searchOption);

            string[] files = [.. aviFiles, .. mkvFiles];
            return files;
        }

        private void UpdateFileList(string path)
        {
            var files = ScanFiles(path);
            Files.Clear();

            foreach (var file in files)
            {
                Files.Add(file);
            }

            CanRun = Files.Any();

        }

        private List<FontFamily> LoadFonts(string filterName)
        {
            var installedFonts = Fonts.GetFontFamilies(_fontDirectory)
                .Where(x =>
                {
                    var fontName = x.Source.Split("#")[^1];
                    return fontName.Contains(filterName, StringComparison.OrdinalIgnoreCase);
                }).ToList();
            return installedFonts;
        }

        private void UpdateFontFamilies(string filterName)
        {
            var scanFonts = LoadFonts(filterName);

            if (!scanFonts.Any())
            {
                MessageBox.Show(
                    "Không tìm thấy font nào",
                    "Thống báo",
                    MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK
                    );
                return;
            }

            FontFamilies.Clear();


            foreach (var fontFamily in scanFonts)
            {
                FontFamilies.Add(fontFamily);
            }

            SubtitleSetting.FontFamily = FontFamilies.FirstOrDefault();

        }
        public override Task LoadAsync()
        {
            UpdateFileList(VideoSetting.InputFolder);

            UpdateFontFamilies(SubtitleSetting.FontSearchText);


            return Task.CompletedTask;
        }
    }
}
