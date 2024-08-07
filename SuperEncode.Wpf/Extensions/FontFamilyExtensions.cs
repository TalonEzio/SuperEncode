﻿using System.Text;
using SuperEncode.Wpf.ViewModels;

namespace SuperEncode.Wpf.Extensions
{
    public static class FontFamilyExtensions
    {
        public static string GetFontName(this SubtitleSetting? subtitleSetting)
        {
            var fontFamily = subtitleSetting?.FontFamily;
            var fontNameBuilder = new StringBuilder(fontFamily?.Source.Split("#")[^1] ?? "Uvn Van");
            return fontNameBuilder.ToString();
        }
    }
}
