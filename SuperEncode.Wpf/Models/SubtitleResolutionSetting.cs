namespace SuperEncode.Wpf.Models;

public class SubtitleResolutionSetting
{
    public SubtitleStyle Marquee { get; set; } = new();
    public SubtitleStyle Logo { get; set; } = new();
    public SubtitleStyle Default { get; set; } = new();
}

public class SubtitleStyle
{
    public Guid Style { get; set; } = Guid.NewGuid();
    public string FontName { get; set; } = string.Empty;
    public int FontSize { get; set; }
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public bool Strikeout { get; set; }
    public (int, int) LogoPosition { get; set; }
}