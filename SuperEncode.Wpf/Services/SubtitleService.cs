using System.Diagnostics;
using System.IO;
using System.Text;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using SuperEncode.Wpf.Extensions;
using SuperEncode.Wpf.ViewModels;

namespace SuperEncode.Wpf.Services
{
    public class SubtitleService
    {
        private static readonly string BasePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()!.Location)!;
        public async Task<string> ConvertToAss(
            string inputFile, SubtitleSetting subtitleSetting, VideoSetting encodeSetting
            )
        {
            var subtitlePath = await ExportSubtitle(inputFile, encodeSetting.EnableCmd);

            if (!await IsAssFormat(subtitlePath))
            {
                subtitlePath = await ConvertToAssInternal(subtitlePath);
            }

            await ProcessSubtitle(subtitlePath, subtitleSetting);

            return subtitlePath;
        }
        private async Task<string> ExportSubtitle(string inputFile, bool enableCmd = false)
        {
            var randomFile = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), ".srt"));

            var arguments = BuildMkvExtractArguments(inputFile, randomFile);

            await RunMkvExtract(arguments, enableCmd);

            return randomFile;
        }

        private string BuildMkvExtractArguments(string inputFile, string outputFile)
        {
            var builder = new StringBuilder();
            builder.Append($" -i \"{inputFile}\" ");
            builder.Append(" -map 0:s:0 ");
            builder.Append($" \"{outputFile}\" ");

            return builder.ToString();
        }

        private async Task RunMkvExtract(string arguments, bool useShellExecute)
        {
            var mkvExtractPath = Path.Combine(BasePath, "Tools", "ffmpeg.exe");
            var startInfo = new ProcessStartInfo
            {
                FileName = mkvExtractPath,
                Arguments = arguments,
                UseShellExecute = useShellExecute,
                CreateNoWindow = !useShellExecute
            };

            using var process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            await process.WaitForExitAsync();
        }


        private Task<string> ConvertToAssInternal(string subtitlePath)
        {
            var outputSubtitlePath = Path.ChangeExtension(subtitlePath, ".ass");
            File.Move(subtitlePath, outputSubtitlePath);
            return Task.FromResult(outputSubtitlePath);
        }

        private async Task ProcessSubtitle(string subtitlePath, SubtitleSetting subtitleSetting)
        {
            var subtitle = Subtitle.Parse(subtitlePath);
            subtitle.RemoveEmptyLines();

            var outputSubtitleContent = new AdvancedSubStationAlpha().ToText(subtitle, subtitlePath);
            await File.WriteAllTextAsync(subtitlePath, outputSubtitleContent);

            if (subtitleSetting.OverrideSubtitle)
            {
                UpdateAssStyle(subtitlePath, subtitleSetting);
            }
        }


        public async Task<bool> IsAssFormat(string filePath)
        {
            var fileContent = await File.ReadAllTextAsync(filePath);
            var hasAssFormat = fileContent.Contains("[Script Info]") && fileContent.Contains("[V4+ Styles]") && fileContent.Contains("[Events]");

            return hasAssFormat;
        }
        public void UpdateAssStyle(string filePath, SubtitleSetting subtitleSetting)
        {
            var logoFontStyle = Guid.NewGuid().ToString();
            var marqueeStyle = Guid.NewGuid().ToString();

            var fontName = subtitleSetting.GetFontName();

            var boldInsert = subtitleSetting.Bold ? -1 : 0;
            var italicInsert = subtitleSetting.Italic ? -1 : 0;
            var underLineInsert = subtitleSetting.Underline ? -1 : 0;
            var strikeoutInsert = subtitleSetting.Strikeout ? -1 : 0;

            var defaultStyleBuilder = $"Style: Default,{fontName}," +
                            $"{subtitleSetting.FontSize}," +
                            $"&H00FFFFFF,&H00000000,&H00000000,&H00000000,{boldInsert},{italicInsert},{underLineInsert},{strikeoutInsert}," +
                            $"100,100,0,0,1,{subtitleSetting.OutLine},0,2,10,10,10,1";

            var marqueeStyleBuilder = $"Style: {marqueeStyle},{fontName}," +
                            $"{subtitleSetting.FontSize * 70 / 100}," +
                            $"&H00FFFFFF,&H000000FF,&H00FFC900,&H00000000,{boldInsert},{italicInsert},{underLineInsert},{strikeoutInsert}," +
                            $"100,100,0,0,3,1,0,8,10,10,3,1";

            var logoStyleBuilder = $"Style: {logoFontStyle},Bowlby One SC," +
                            $"{subtitleSetting.FontSize * 85 / 100}," +
                            $"&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0,100,100,0.75,0,1,0.5,0.2,2,10,10,10,1";

            var newStyles = $"{defaultStyleBuilder}\n{marqueeStyleBuilder}\n{logoStyleBuilder}\n";

            var eventBuilder = new StringBuilder();

            if (!string.IsNullOrEmpty(subtitleSetting.Website))
            {
                eventBuilder.AppendLine(
                    $"\nDialogue: 0,0:00:00.00,5:00:00.00,{logoFontStyle},,0,0,0,,{{\\pos(343.6,35.733)}}{subtitleSetting.Website.ToUpper()}");
            }

            eventBuilder.AppendLine(
                $"Dialogue: 0,0:00:00.00,0:02:00.00,{marqueeStyle},,0,0,0,Banner;60;0;300[delay;left to right;fadeawaywidth;],{subtitleSetting.Marquee}");
            eventBuilder.AppendLine(
                $"Dialogue: 0,0:11:00.00,0:13:00.00,{marqueeStyle},,0,0,0,Banner;60;0;300[delay;left to right;fadeawaywidth;],{subtitleSetting.Marquee}");

            var newEvents = eventBuilder.ToString();

            var fileContent = File.ReadAllText(filePath);

            var beginStyleIndex = fileContent.IndexOf(
                "Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding",
                StringComparison.OrdinalIgnoreCase);

            if (beginStyleIndex >= 0)
            {

                var endOfFormatIndex = fileContent.IndexOf("[Events]", beginStyleIndex, StringComparison.Ordinal);
                if (endOfFormatIndex >= 0)
                {
                    fileContent = fileContent.Insert(endOfFormatIndex - 2, newStyles);
                }
            }

            var beginEventIndex =
                fileContent.IndexOf("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text", StringComparison.Ordinal);

            if (beginEventIndex >= 0)
            {
                var endOfFormatIndex = fileContent.IndexOf('\n', beginEventIndex);

                if (endOfFormatIndex >= 0)
                {
                    fileContent = fileContent.Insert(endOfFormatIndex + 1, newEvents);
                }
            }
            File.WriteAllText(filePath, fileContent);
        }
    }
}
