using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuperEncode.Wpf.ViewModels;

public partial class EncodeVideoInput : ObservableObject
{
    private readonly Stopwatch _stopwatch = new();

    [ObservableProperty] public TimeSpan _duration = TimeSpan.Zero;

    [ObservableProperty] private string _filePath = string.Empty;

    [ObservableProperty] private double _percent;

    private Timer? _timer;


    public void Start()
    {
        _stopwatch.Start();

        _timer = new Timer(_ =>
                Duration = _stopwatch.Elapsed
            , null, 100, 100);
    }

    public async Task Stop()
    {
        _stopwatch.Stop();
        Duration = _stopwatch.Elapsed;
        if (_timer != null) await _timer.DisposeAsync();
    }
}