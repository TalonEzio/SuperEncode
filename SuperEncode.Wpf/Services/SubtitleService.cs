using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using SuperEncode.Wpf.Extensions;
using SuperEncode.Wpf.Models;
using SuperEncode.Wpf.ViewModels;
using Xabe.FFmpeg;

namespace SuperEncode.Wpf.Services
{
    public class SubtitleService
    {
        private static readonly string BasePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()!.Location)!;
        public async Task<string> ConvertToAss(
            IMediaInfo mediaInfo, SubtitleSetting subtitleSetting)
        {
            var inputFile = mediaInfo.Path;
            var subtitlePath = await ExportSubtitle(inputFile,enableCmd:false);

            await UpdateAssStyle(subtitlePath, subtitleSetting);
            return subtitlePath;
        }
        private static async Task<string> ExportSubtitle(string inputFile, bool enableCmd = false)
        {
            var randomFile = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), ".ass"));

            var arguments = BuildExportSubtitleArguments(inputFile, randomFile);

            await RunExportSubtitle(arguments, enableCmd);

            return randomFile;
        }

        private static string BuildExportSubtitleArguments(string inputFile, string outputFile)
        {
            var builder = new StringBuilder();
            builder.Append($" -i \"{inputFile}\" ");
            builder.Append(" -map 0:s:0 ");
            builder.Append($" \"{outputFile}\" ");
            return builder.ToString();
        }

        private static async Task RunExportSubtitle(string arguments, bool enableCmd)
        {
            var ffmpegPath = Path.Combine(BasePath, "Tools", "ffmpeg.exe");
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = !enableCmd
            };

            using var process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            await process.WaitForExitAsync();
        }

        private string FindStyleLine(string fileContent, string styleName)
        {
            var startPosition = fileContent.IndexOf($"Style: {styleName}", StringComparison.OrdinalIgnoreCase);

            var endPosition = fileContent.IndexOf("\n", startPosition, StringComparison.OrdinalIgnoreCase);

            return fileContent.Substring(startPosition, endPosition - startPosition - 1);
        }
        private AssStyleFormat ReadStyleFromLine(string line)
        {
            var split = line.Split(',');

            var result = new AssStyleFormat()
            {
                Name = split[0].Replace("Style:", "").Trim(),
                FontName = split[1],
                FontSize = int.Parse(split[2]),
                PrimaryColour = split[3],
                SecondaryColour = split[4],
                OutlineColour = split[5],
                BackColour = split[6],
                Bold = int.Parse(split[7]) == -1,
                Italic = int.Parse(split[8]) == -1,
                Underline = int.Parse(split[9]) == -1,
                StrikeOut = int.Parse(split[10]) == -1,
                ScaleX = int.Parse(split[11]),
                ScaleY = int.Parse(split[12]),
                Spacing = int.Parse(split[13]),
                Angle = int.Parse(split[14]),
                BorderStyle = int.Parse(split[15]),
                Outline = double.Parse(split[16]),
                Shadow = double.Parse(split[17]),
                Alignment = int.Parse(split[18]),
                MarginL = int.Parse(split[19]),
                MarginR = int.Parse(split[20]),
                MarginV = int.Parse(split[21]),
                Encoding = (StyleEncoding)Enum.Parse(typeof(StyleEncoding), split[22])
            };

            return result;
        }


        public async Task UpdateAssStyle(string filePath, SubtitleSetting subtitleSetting)
        {
            var resultBuilder = new StringBuilder();

            var templatePath = Path.Combine(BasePath, "AssStyles", "template.ass");
            var templateContent = await File.ReadAllTextAsync(templatePath);

            var inputContent = await File.ReadAllTextAsync(filePath);

            templateContent = templateContent.Replace("[[[Website]]]", subtitleSetting.Website.ToUpper());
            templateContent = templateContent.Replace("[[[Marquee]]]", subtitleSetting.Marquee);
            templateContent = templateContent.Replace("[[[Marquee-FontName]]]", subtitleSetting.GetFontName());
            templateContent = templateContent.Replace("[[[Marquee-FontSize]]]", (subtitleSetting.FontSize * 7.5 / 10).ToString(CultureInfo.InvariantCulture));

            const string beginStyleString = "Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding";

            var beginTemplateStyleIndex = templateContent.IndexOf(beginStyleString, StringComparison.OrdinalIgnoreCase);

            var endTemplateStyleIndex = templateContent.IndexOf("\n", beginTemplateStyleIndex, StringComparison.Ordinal);

            if (subtitleSetting.OverrideStyleDefault)
            {
                var defaultStyle = ReadStyleFromLine(FindStyleLine(templateContent, "Default"));

                defaultStyle.FontName = subtitleSetting.GetFontName();
                defaultStyle.FontSize = subtitleSetting.FontSize;
                defaultStyle.Bold = subtitleSetting.Bold;
                defaultStyle.Italic = subtitleSetting.Italic;
                defaultStyle.Underline = subtitleSetting.Underline;
                defaultStyle.StrikeOut = subtitleSetting.Strikeout;
                defaultStyle.Outline = subtitleSetting.OutLine;

                var beginDefaultStyle = templateContent.IndexOf("Style: Default", StringComparison.OrdinalIgnoreCase);

                var endDefaultStyle =
                    templateContent.IndexOf("\n", beginDefaultStyle, StringComparison.OrdinalIgnoreCase);

                templateContent = templateContent.Remove(beginDefaultStyle, endDefaultStyle - beginDefaultStyle);
                templateContent = templateContent.Insert(endTemplateStyleIndex + 1, defaultStyle.ToString());
            }


            var beginDialogueIndex =
                inputContent.IndexOf(
                    "Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text",
                    StringComparison.OrdinalIgnoreCase);

            var endDialogueIndex = inputContent.IndexOf("\n", beginDialogueIndex, StringComparison.OrdinalIgnoreCase);

            var allDialogueFromSubtitles = inputContent[(endDialogueIndex + 1)..];

            var beginInputStyleIndex = inputContent.IndexOf(beginStyleString, StringComparison.OrdinalIgnoreCase);

            var endInputStyleIndex = inputContent.IndexOf("\n", beginInputStyleIndex, StringComparison.OrdinalIgnoreCase);

            var beginInputEventIndex = inputContent.IndexOf("[Events]", StringComparison.OrdinalIgnoreCase);
            var inputStyleString = inputContent.Substring(endInputStyleIndex + 1, beginInputEventIndex - endInputStyleIndex - 1).TrimEnd('\n')+'\n';

            var beginTemplateEventIndex = templateContent.IndexOf("[Events]", StringComparison.OrdinalIgnoreCase);

            if (subtitleSetting.OverrideStyleDefault)
            {
                var beginDefaultStyleIndex =
                    inputStyleString.IndexOf("Style: Default", StringComparison.OrdinalIgnoreCase);
                var endDefaultStyleIndex = inputStyleString.IndexOf("\n",beginDefaultStyleIndex,StringComparison.OrdinalIgnoreCase);

                inputStyleString = inputStyleString.Remove(beginDefaultStyleIndex, endDefaultStyleIndex - beginDefaultStyleIndex);
            }
            templateContent = templateContent.Insert(beginTemplateEventIndex - 2, inputStyleString);

            resultBuilder.AppendLine(templateContent);

            resultBuilder.Append(allDialogueFromSubtitles);

            await File.WriteAllTextAsync(filePath, resultBuilder.ToString());
        }
    }
}
