using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Notification.Wpf;
using SuperEncode.Wpf.Models;
using SuperEncode.Wpf.Services;
using SuperEncode.Wpf.UserControls;

namespace SuperEncode.Wpf.ViewModels;

public partial class MainViewModel(
    VideoService videoService,
    SubtitleService subtitleService,
    INotificationManager notificationManager) : ObservableObject
{
    private const string FontDirectory = @"C:\Windows\Fonts";
    public static Process? RunningProcess;

    private CancellationTokenSource _cancellationTokenSource = new();

    [ObservableProperty] private bool _canRun;

    [ObservableProperty] private bool _enableWindow = true;


    [ObservableProperty] private CurrentVideoPlayerData _playerData = new();

    [ObservableProperty] private SubtitleSetting _subtitleSetting = new();

    [ObservableProperty] private int _successCount;

    [ObservableProperty] private double _successPercent;

    [ObservableProperty] private VideoSetting _videoSetting = new();

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
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(SuperEncode),
                    $"{nameof(SuperEncode)}-Config.json");
            await SaveConfigToFile(configPath);

            if (RunningProcess is { HasExited: false }) RunningProcess.Kill();
        }

        catch (Exception e)
        {
            MessageBox.Show("Lỗi khi lưu cấu hình: " + e.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public async Task PlayPreview(object[] paramsObjects)
    {
        try
        {
            var input = paramsObjects[0] as EncodeVideoInput;

            var videoPlayer = paramsObjects[1] as UcVideoPlayer;

            videoPlayer!.VideoView.MediaPlayer!.Stop();

            await Task.Delay(500);

            if (File.Exists(PlayerData.SubtitleUrl)) File.Delete(PlayerData.SubtitleUrl);

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
        catch (Exception e)
        {
            PlayErrorSound("Lỗi " + e.Message);
        }
    }

    [RelayCommand]
    public void ScanDeep()
    {
        UpdateFileList(VideoSetting.InputFolder);
    }

    private bool CanRunEncode()
    {
        return CanRun;
    }


    [RelayCommand(CanExecute = nameof(CanRunEncode))]
    private async Task RunEncode()
    {
        SuccessCount = 0;
        PlayerData.SubtitleUrl = PlayerData.VideoUrl = string.Empty;

        UpdateFileList(VideoSetting.InputFolder);

        DurationStopwatch.Restart();

        UpdateEnableWindow(false);

        var timer = new Timer(_ =>
            OnPropertyChanged(nameof(DurationStopwatch)), null, 100, 100);

        var userCancelProcessing = false;
        foreach (var input in VideoInputs)
            try
            {
                await ProcessFile(input);
            }
            catch (FileNotFoundException)
            {
                PlayErrorSound($"Encode lỗi, không thấy file: {Path.GetFileName(input.FilePath)}!");
            }
            catch (TaskCanceledException)
            {
                PlayWarningSound($"Encode bị hủy: {Path.GetFileName(input.FilePath)}!");
                userCancelProcessing = true;
            }
            catch (Exception e)
            {
                PlayErrorSound($"Encode lỗi {Path.GetFileName(input.FilePath)}!\n{e.Message}");
            }
            finally
            {
                SuccessCount++;
                await input.Stop();
            }

        DurationStopwatch.Stop();
        await timer.DisposeAsync();

        if (!userCancelProcessing) PlaySuccessSound("Xử lý hoàn tất");

        UpdateEnableWindow(true);
    }

    private void PlaySuccessSound(string message)
    {
        PlaySound(message, NotificationType.Success);
    }

    private void PlayWarningSound(string message)
    {
        PlaySound(message, NotificationType.Warning);
    }

    private void PlayErrorSound(string message)
    {
        PlaySound(message, NotificationType.Error);
    }

    public void PlaySound(string message, NotificationType notificationType)
    {
        var soundFile = notificationType switch
        {
            NotificationType.Error => "sound-error",
            NotificationType.Warning => "sound-warning",
            _ => "sound-success"
        };
        soundFile = Path.ChangeExtension(soundFile, ".wav");

        var soundFilePath = Path.Combine(AppContext.BaseDirectory, "AssStyles", soundFile);

        var player = new SoundPlayer(soundFilePath);
        player.Play();

        if (!string.IsNullOrWhiteSpace(message)) notificationManager.Show(message, notificationType);
    }

    private bool CanReset()
    {
        return EnableWindow;
    }

    [RelayCommand(CanExecute = nameof(CanReset))]
    private void Reset()
    {
        VideoSetting.InputFolder = "";

        Files.Clear();

        UpdateCanRunEncode(Files.Any());

        PlayerData.SubtitleUrl = PlayerData.VideoUrl = string.Empty;

        RunEncodeCommand.NotifyCanExecuteChanged();
    }

    public bool CanCancelEncode()
    {
        return !EnableWindow;
    }

    [RelayCommand(CanExecute = nameof(CanCancelEncode))]
    public async Task CancelEncode()
    {
        await _cancellationTokenSource.CancelAsync();

        _cancellationTokenSource = new CancellationTokenSource();
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
        if (File.Exists(configPath)) File.Delete(configPath);
        await using var fileStream = new FileStream(configPath, FileMode.CreateNew);
        await using var writerStream = new StreamWriter(fileStream, Encoding.Unicode);

        var settings = new SettingJson
        {
            VideoSetting = VideoSetting,
            SubtitleSetting = SubtitleSetting
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

        foreach (var file in files) Files.Add(file);

        UpdateCanRunEncode(Files.Any());

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


        foreach (var fontFamily in scanFonts) FontFamilies.Add(fontFamily);

        SubtitleSetting.FontFamily = FontFamilies.FirstOrDefault();
    }

    private async Task ProcessFile(EncodeVideoInput input)
    {
        if (!File.Exists(input.FilePath))
        {
            ShowErrorMessage($"Không tìm thấy file {Path.GetFileName(input.FilePath)}, vui lòng xem lại!");
            return;
        }

        input.Percent = 0;
        input.Start();

        await videoService.EncodeVideoWithNVencC(input, SubtitleSetting, VideoSetting, _cancellationTokenSource.Token);

        input.Percent = 100;

        _cancellationTokenSource = new CancellationTokenSource();
    }

    private void UpdateEnableWindow(bool value)
    {
        EnableWindow = value;
        ResetCommand.NotifyCanExecuteChanged();
        CancelEncodeCommand.NotifyCanExecuteChanged();
    }

    private void UpdateCanRunEncode(bool value)
    {
        CanRun = value;
        CancelEncodeCommand.NotifyCanExecuteChanged();
    }


    private static void ShowErrorMessage(string message)
    {
        MessageBox.Show(message, "Thống báo", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
    }

    public Task LoadAsync()
    {
        UpdateFileList(VideoSetting.InputFolder);

        UpdateFontFamilies(SubtitleSetting.FontSearchText!);

        Files.CollectionChanged += Files_CollectionChanged;

        return Task.CompletedTask;
    }

    private void Files_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            if (e.NewItems == null) return;

            foreach (string newFile in e.NewItems)
                VideoInputs.Add(new EncodeVideoInput
                    {
                        FilePath = newFile,
                        Percent = 0
                    }
                );
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            if (e.OldItems == null) return;
            foreach (string oldFile in e.OldItems)
            {
                var itemToRemove = VideoInputs.FirstOrDefault(vi => vi.FilePath == oldFile);
                if (itemToRemove != null) VideoInputs.Remove(itemToRemove);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            VideoInputs.Clear();
        }
    }

    private static List<FontFamily> LoadFonts(string filterName)
    {
        var installedFonts = Fonts.GetFontFamilies(FontDirectory)
            .Where(x =>
            {
                var fontName = x.Source.Split("#")[^1];
                return string.IsNullOrEmpty(filterName) ||
                       fontName.Contains(filterName, StringComparison.OrdinalIgnoreCase);
            }).ToList();
        return installedFonts;
    }
}