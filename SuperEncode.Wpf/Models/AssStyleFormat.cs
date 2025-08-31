namespace SuperEncode.Wpf.Models;

public class AssStyleFormat
{
    public string Name { get; set; } = Guid.NewGuid().ToString();
    public string FontName { get; set; } = string.Empty;
    public int FontSize { get; set; }
    public string PrimaryColour { get; set; } = string.Empty;
    public string SecondaryColour { get; set; } = string.Empty;
    public string OutlineColour { get; set; } = string.Empty;
    public string BackColour { get; set; } = string.Empty;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public bool StrikeOut { get; set; }
    public int BoldBuilder => Bold ? -1 : 0;
    public int ItalicBuilder => Italic ? -1 : 0;
    public int UnderlineBuilder => Underline ? -1 : 0;
    public int StrikeOutBuilder => StrikeOut ? -1 : 0;
    public int ScaleX { get; set; } = 100;
    public int ScaleY { get; set; } = 100;
    public int Spacing { get; set; }
    public int Angle { get; set; }
    public int BorderStyle { get; set; }
    public double Outline { get; set; } = 1;
    public double Shadow { get; set; } = 1;
    public int Alignment { get; set; }
    public int MarginL { get; set; } = 10;
    public int MarginR { get; set; } = 10;
    public int MarginV { get; set; } = 10;
    public StyleEncoding Encoding { get; set; } = StyleEncoding.Default;

    public override string ToString()
    {
        return $"Style: {Name},{FontName},{FontSize},{PrimaryColour},{SecondaryColour},{OutlineColour},{BackColour}," +
               $"{BoldBuilder},{ItalicBuilder},{UnderlineBuilder},{StrikeOutBuilder},{ScaleX},{ScaleY},{Spacing},{Angle},{BorderStyle}," +
               $"{Outline},{Shadow},{Alignment},{MarginL},{MarginR},{MarginV},{(int)Encoding}";
    }
}

public enum StyleEncoding
{
    Ansi = 0,
    Default = 1,
    Symbol = 2,
    Mac = 77,
    ShiftJis = 128,
    Hangeul = 129,
    Johab = 130,
    Gb2312 = 134,
    ChineseBig5 = 1365,
    Greek = 161,
    Turkish = 162,
    Vietnamese = 163,
    Hebrew = 177,
    Arabic = 178,
    Baltic = 186,
    Russian = 204,
    Thai = 222,
    EaseEuropean = 238,
    Oem = 255
}