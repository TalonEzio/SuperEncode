namespace SuperEncode.Wpf.Extensions;

public class VideoProcessEventArgs : EventArgs
{
    public string FilePath { get; set; } = string.Empty;
    public double Percentage { get; set; }
}