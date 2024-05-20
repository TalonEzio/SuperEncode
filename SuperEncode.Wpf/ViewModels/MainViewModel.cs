using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SuperEncode.Wpf.Extensions;
using SuperEncode.Wpf.Services;
using SuperEncode.Wpf.Models;

namespace SuperEncode.Wpf.ViewModels
{
    public partial class MainViewModel(VideoService videoService) : ObservableObject
    {
        private const string FontDirectory = @"C:\Windows\Fonts";

        [ObservableProperty]
        private SubtitleSetting _subtitleSetting = new();

        [ObservableProperty]
        private VideoSetting _videoSetting = new();

        [ObservableProperty]
        private double _successPercent;

        [ObservableProperty]
        private int _successCount;

        [ObservableProperty]
        private bool _canRun;

        [ObservableProperty]
        private bool _enableWindow = true;


        public ObservableCollection<string> Files { get; set; } = [];
        public ObservableCollection<FontFamily> FontFamilies { get; set; } = [];
        
        public Stopwatch DurationStopwatch { get; } = new();

        [RelayCommand]
        private async Task LoadedForm(object obj)
        {
            await LoadAsync();
        }

        [RelayCommand]
        private async Task ClosingForm(object obj)
        {
            try
            {
                var configPath =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(SuperEncode), $"{nameof(SuperEncode)}-Config.json");
                await SaveConfigToFile(configPath);
            }
            catch (Exception e)
            {
                MessageBox.Show("Lỗi khi lưu cấu hình: " + e.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private async Task SaveConfigToFile(string configPath)
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
            await using var fileStream = new FileStream(configPath, FileMode.CreateNew);
            await using var writerStream = new StreamWriter(fileStream, Encoding.Unicode);

            var fontSearchText = SubtitleSetting.GetFontName();

            if (fontSearchText.Contains("Bold")) SubtitleSetting.FontSearchText = fontSearchText.Replace("Bold", "").TrimEnd();


            var settings = new SettingJson()
            {
                VideoSetting = VideoSetting,
                SubtitleSetting = SubtitleSetting,
            };
            
            var json = JsonSerializer.Serialize(settings);
            await writerStream.WriteAsync(json);
            writerStream.Close();
            fileStream.Close();
        }

        private bool CanRunEncode(object? arg) => CanRun;

        [RelayCommand(CanExecute = nameof(CanRunEncode))]
        private async Task RunEncode(object? obj)
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

            SuccessPercent = 0;

            var outputVideoFile =
                await videoService.EncodeVideoWithNVencC(
                file, SubtitleSetting, VideoSetting);

            if (new FileInfo(outputVideoFile).Length == 0)
            {
                ShowErrorMessage($"Encode lỗi {Path.GetFileName(file)}, vui lòng xem lại!");
            }
            else
            {
                SuccessPercent = 100;
            }
        }

        private static void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Thống báo", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
        }

        [RelayCommand]
        private void Reset()
        {
            VideoSetting.InputFolder = "";

            Files.Clear();

            CanRun = Files.Any();
        }

        [RelayCommand]
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

        [RelayCommand]
        private void SelectPath()
        {

            var folderDialog = new OpenFolderDialog();
            folderDialog.ShowDialog();

            VideoSetting.InputFolder = folderDialog.FolderName;

            UpdateFileList(VideoSetting.InputFolder);
        }

        private IEnumerable<string> ScanFiles(string path)
        {
            if (!Directory.Exists(path)) return [];

            var searchOption = VideoSetting.ScanDeep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
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
        private static List<FontFamily> LoadFonts(string filterName)
        {
            var installedFonts = Fonts.GetFontFamilies(FontDirectory)
                .Where(x =>
                {
                    var fontName = x.Source.Split("#")[^1];
                    return string.IsNullOrEmpty(filterName) || fontName.Contains(filterName, StringComparison.OrdinalIgnoreCase);
                }).ToList();
            return installedFonts;
        }

        [RelayCommand]
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
        public Task LoadAsync()
        {
            UpdateFileList(VideoSetting.InputFolder);

            UpdateFontFamilies(SubtitleSetting.FontSearchText!);

            videoService.VideoEventHandler += VideoEventHandler;
            return Task.CompletedTask;
        }

        private void VideoEventHandler(object? sender, VideoProcessEventArgs e)
        {
            SuccessPercent = e.Percentage;
        }
    }
}
