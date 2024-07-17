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
using SuperEncode.Wpf.UserControls;
using Notification.Wpf;
using System.Media;

namespace SuperEncode.Wpf.ViewModels
{
    public partial class MainViewModel(VideoService videoService, SubtitleService subtitleService,INotificationManager notificationManager) : ObservableObject
    {
        private CancellationTokenSource _cancellationTokenSource = new();
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

        [ObservableProperty]
        private bool _enablePreview = true;

        [ObservableProperty]
        private CurrentVideoPlayerData _playerData = new();

        public ObservableCollection<string> Files { get; set; } = [];

        public ObservableCollection<EncodeVideoInput> VideoInputs { get; set; } = [];
        public ObservableCollection<FontFamily> FontFamilies { get; set; } = [];

        public Stopwatch DurationStopwatch { get; } = new();

        [RelayCommand]
        private async Task LoadedForm()
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

                if (_runningProcess is { HasExited: false })
                {
                    _runningProcess.Kill();
                }
            }

            catch (Exception e)
            {
                MessageBox.Show("Lỗi khi lưu cấu hình: " + e.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        [RelayCommand]
        public async Task PlayPreview(object[] paramsObjects)
        {
            var input = paramsObjects[0] as EncodeVideoInput;

            var videoPlayer = paramsObjects[1] as UcVideoPlayer;

            videoPlayer!.VideoView.MediaPlayer!.Stop();

            await Task.Delay(500);

            if (File.Exists(PlayerData.SubtitleUrl))
            {
                File.Delete(PlayerData.SubtitleUrl);
            }

            if (!File.Exists(input?.FilePath)) return;

            PlayerData.VideoUrl = input.FilePath;
            OnPropertyChanged(PlayerData.VideoUrl);

            var tempSubtitleFromVideo = await subtitleService.GetSubtitleFromVideo(
                input.FilePath,
                SubtitleSetting,
                _cancellationTokenSource.Token
                );

            if (!string.IsNullOrEmpty(tempSubtitleFromVideo))
            {
                PlayerData.SubtitleUrl = tempSubtitleFromVideo;
                OnPropertyChanged(PlayerData.SubtitleUrl);
            }


            _cancellationTokenSource = new CancellationTokenSource();
        }

        [RelayCommand]
        public void ScanDeep()
        {
            UpdateFileList(VideoSetting.InputFolder);
        }
        private bool CanRunEncode() => CanRun;


        [RelayCommand(CanExecute = nameof(CanRunEncode))]
        private async Task RunEncode()
        {
            PlayerData.SubtitleUrl = PlayerData.VideoUrl = string.Empty;
            EnablePreview = false;

            UpdateFileList(VideoSetting.InputFolder);

            DurationStopwatch.Restart();

            UpdateEnableWindow(false);

            var timer = new Timer(_ =>
                OnPropertyChanged(nameof(DurationStopwatch)), null, 10, 10);

            foreach (var input in VideoInputs)
            {
                try
                {
                    await ProcessFile(input);
                }
                catch (FileNotFoundException)
                {
                    ShowErrorMessage($"Encode lỗi {Path.GetFileName(input.FilePath)}, file không thấy!");
                }
                catch (Exception e)
                {
                    ShowErrorMessage($"Encode lỗi {Path.GetFileName(input.FilePath)}!\n{e.Message}");
                }
                finally
                {
                    SuccessCount++;
                }
            }

            DurationStopwatch.Stop();

            //MessageBox.Show(
            //    "Xử lý hoàn tất!",
            //    "Thống báo",
            //    MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK
            //);

            var content = new NotificationContent();
            notificationManager.Show("Thông báo", "Xử lý hoàn tất",NotificationType.Success);
            PlayNotificationSound();
            await timer.DisposeAsync();

            SuccessCount = 0;
            EnablePreview = true;

            UpdateEnableWindow(true);
        }

        private void PlayNotificationSound()
        {

            try
            {
                string soundFilePath = Path.Combine(AppContext.BaseDirectory,"AssStyles", "sound-success.wav");

                var player = new SoundPlayer(soundFilePath);
                
                player.Play();
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu có
                MessageBox.Show("Không thể phát âm thanh: " + ex.Message);
            }
        }


        private bool CanReset() => EnableWindow;
        [RelayCommand(CanExecute = nameof(CanReset))]
        private void Reset()
        {
            VideoSetting.InputFolder = "";

            Files.Clear();

            VideoInputs.Clear();

            CanRun = Files.Any();

            PlayerData.SubtitleUrl = PlayerData.VideoUrl = String.Empty;

            RunEncodeCommand.NotifyCanExecuteChanged();

        }

        public bool CanCancelEncode() => EnableWindow == false;
        [RelayCommand(CanExecute = nameof(CanCancelEncode))]
        public async Task CancelEncode()
        {
            await _cancellationTokenSource.CancelAsync();

            _cancellationTokenSource = new();
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


        private async Task SaveConfigToFile(string configPath)
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
            await using var fileStream = new FileStream(configPath, FileMode.CreateNew);
            await using var writerStream = new StreamWriter(fileStream, Encoding.Unicode);

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

            if (Files.Any())
            {
                VideoInputs.Clear();

                foreach (var file in Files)
                {
                    VideoInputs.Add(new EncodeVideoInput()
                    {
                        FilePath = file,
                        Percent = 0,
                    });
                }

            }

            EnablePreview = CanRun = Files.Any();
            RunEncodeCommand.NotifyCanExecuteChanged();


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



        async Task ProcessFile(EncodeVideoInput input)
        {
            if (!File.Exists(input.FilePath))
            {
                ShowErrorMessage($"Không tìm thấy file {Path.GetFileName(input.FilePath)}, vui lòng xem lại!");
                return;
            }

            input.Percent = 0;

            await videoService.EncodeVideoWithNVencC(
                input, SubtitleSetting, VideoSetting, _cancellationTokenSource.Token);

            input.Percent = 100;

            _cancellationTokenSource = new();
        }

        private void UpdateEnableWindow(bool value)
        {
            EnableWindow = value;
            ResetCommand.NotifyCanExecuteChanged();
            CancelEncodeCommand.NotifyCanExecuteChanged();
        }
        private static Process? _runningProcess;
        private static void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Thống báo", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
        }
        private void VideoEventHandler(object? sender, VideoProcessEventArgs e)
        {
            _runningProcess = sender as Process;
            SuccessPercent = e.Percentage;
        }
        public Task LoadAsync()
        {
            UpdateFileList(VideoSetting.InputFolder);

            UpdateFontFamilies(SubtitleSetting.FontSearchText!);

            videoService.VideoEventHandler += VideoEventHandler;
            return Task.CompletedTask;
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

    }
}
