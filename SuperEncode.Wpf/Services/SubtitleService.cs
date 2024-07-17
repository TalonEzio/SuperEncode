using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using SuperEncode.Wpf.Extensions;
using SuperEncode.Wpf.Models;
using SuperEncode.Wpf.ViewModels;

namespace SuperEncode.Wpf.Services
{
    public class SubtitleService
    {
        private static readonly string BasePath = AppContext.BaseDirectory;
        public async Task<string> GetSubtitleFromVideo(
            string path, SubtitleSetting subtitleSetting, CancellationToken cancellation = default)
        {
            var subtitlePath = string.Empty;
            try
            {
                while (!cancellation.IsCancellationRequested)
                {
                    if (subtitleSetting.SubtitleInFile)
                    {
                        subtitlePath = await ExportSubtitleFromVideo(path, enableCmd: false);
                    }
                    else
                    {
                        subtitlePath = await ScanSubtitleFromFolder(path, subtitleSetting.SuffixSubtitle);

                    }

                    if (!string.IsNullOrEmpty(subtitleSetting.Marquee) && !string.IsNullOrEmpty(subtitleSetting.Website))
                    {
                        await UpdateAssStyle(subtitlePath, subtitleSetting);
                    }

                    return subtitlePath;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Cancel from get subtitle");
                if (File.Exists(subtitlePath)) File.Delete(subtitlePath);
            }
            return subtitlePath;
        }

        private async Task<string> ScanSubtitleFromFolder(string inputFile, string suffixString)
        {
            var fileInfo = new FileInfo(inputFile);
            var subtitleDirectory = fileInfo.DirectoryName!;

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.Name);

            var suffixList = new List<string> { "" };
            suffixList.AddRange(suffixString.Split(',').Where(x => !string.IsNullOrEmpty(x)));

            string[] subtitleExtensions = [".ass", ".srt", ".vtt"];

            Debug.WriteLine(subtitleDirectory);
            foreach (var subtitleExtension in subtitleExtensions)
            {
                foreach (var subtitleFileName in suffixList.Select(suffixFileName => $"{fileNameWithoutExtension}{suffixFileName}{subtitleExtension}"))
                {
                    Debug.WriteLine($"Info: {subtitleFileName}");

                    var subtitleFullPath = Path.Combine(subtitleDirectory, subtitleFileName);
                    try
                    {
                        if (!File.Exists(subtitleFullPath)) continue;

                        var actuallySubtitleFullPath = await ConvertSubtitleToAss(subtitleFullPath);

                        return actuallySubtitleFullPath;
                    }
                    catch (FileNotFoundException e)
                    {
                        Debug.WriteLine($"Error: {e.Message}");
                    }
                }
            }

            throw new FileNotFoundException("Không tìm thấy phụ đề nào!");

        }

        private static async Task<string> ConvertSubtitleToAss(string subtitleFullPath)
        {
            var actuallySubtitleFullPath = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetTempFileName(), ".ass"));

            var arguments = BuildConvertSubtitleArguments(subtitleFullPath, actuallySubtitleFullPath);
            await RunFfmpegWithArguments(arguments, false);

            if (subtitleFullPath.EndsWith(".ass"))
                return actuallySubtitleFullPath;


            var fileContent = await File.ReadAllTextAsync(actuallySubtitleFullPath);

            fileContent = fileContent
                .Replace("PlayResX: 384", "PlayResX: 1920")
                .Replace("PlayResY: 288", "PlayResY: 1080")
                .Replace(
                "Style: Default,Arial,16,&Hffffff,&Hffffff,&H0,&H0,0,0,0,0,100,100,0,0,1,1,0,2,10,10,10,1",
                "Style: Default,Arial,65,&H00FFFFFF,&H00FFFFFF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,2,1,2,10,10,30,1");

            await File.WriteAllTextAsync(actuallySubtitleFullPath, fileContent);

            return actuallySubtitleFullPath;

        }

        private static async Task<string> ExportSubtitleFromVideo(string inputFile, bool enableCmd = false)
        {
            var randomFile = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), ".ass"));

            var arguments = BuildExportSubtitleArguments(inputFile, randomFile);

            await RunFfmpegWithArguments(arguments, enableCmd);

            return randomFile;
        }

        private static string BuildConvertSubtitleArguments(string inputFile, string outputFile)
        {
            var builder = new StringBuilder();
            builder.Append($" -i \"{inputFile}\" ");
            builder.Append($" \"{outputFile}\" ");
            return builder.ToString();
        }
        private static string BuildExportSubtitleArguments(string inputFile, string outputFile)
        {
            var builder = new StringBuilder();
            builder.Append($" -i \"{inputFile}\" ");
            builder.Append(" -map 0:s:0 ");
            builder.Append($" \"{outputFile}\" ");
            return builder.ToString();
        }

        private static async Task RunFfmpegWithArguments(string arguments, bool enableCmd)
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

            var outputContent = await File.ReadAllTextAsync(templatePath);

            var inputContent = await File.ReadAllTextAsync(filePath);

            outputContent = outputContent
                .Replace("[[[PlayResX]]]", "1920")
                .Replace("[[[PlayResY]]]", "1080")
                .Replace("[[[Website]]]", subtitleSetting.Website.ToUpper())
                .Replace("[[[Marquee]]]", subtitleSetting.Marquee)
                .Replace("[[[Marquee-FontName]]]", subtitleSetting.GetFontName())
                .Replace("[[[Marquee-FontSize]]]", (subtitleSetting.FontSize * 7.5 / 10).ToString(CultureInfo.InvariantCulture));

            const string beginStyleString = "[V4+ Styles]";


            #region Get All Dialogue From Subtitle

            const string beginEventString = "[Events]";

            var beginEventIndex =
                inputContent.IndexOf(beginEventString, StringComparison.OrdinalIgnoreCase);

            var endEventIndex = inputContent.IndexOf("\n", beginEventIndex + beginEventString.Length, StringComparison.OrdinalIgnoreCase);
            endEventIndex += 1;//skip \n

            var beginDialogueIndex = inputContent.IndexOf("\n", endEventIndex + 1, StringComparison.OrdinalIgnoreCase);
            beginDialogueIndex += 1; //skip \n

            var allDialogueFromSubtitles = inputContent[beginDialogueIndex..];

            #endregion

            #region Insert Another Style To Output File

            var beginInputStyleIndex = inputContent.IndexOf(beginStyleString, StringComparison.OrdinalIgnoreCase);


            var endInputStyleIndex = inputContent.IndexOf("\n", beginInputStyleIndex, StringComparison.OrdinalIgnoreCase);

            var beginInputEventIndex = inputContent.IndexOf(beginEventString, StringComparison.OrdinalIgnoreCase);

            var inputStyleArray =
                inputContent[(endInputStyleIndex + 1)..(beginInputEventIndex)]
                    .Split("\n")
                    .Where(style => style.StartsWith("Style")).ToList();



            var beginTemplateEventIndex = outputContent.IndexOf(beginEventString, StringComparison.OrdinalIgnoreCase);

            if (subtitleSetting.OverrideStyleDefault)
            {
                var defaultStyle = inputStyleArray.FirstOrDefault(x => x.StartsWith("Style: Default,"));
                if (defaultStyle != null)
                {
                    inputStyleArray.Remove(defaultStyle);
                }
            }

            var inputStyleString = string.Join("\n", inputStyleArray);

            outputContent = outputContent.Insert(beginTemplateEventIndex - 2, inputStyleString);

            #endregion

            if (subtitleSetting.OverrideStyleDefault)
            {
                var defaultStyle = ReadStyleFromLine(FindStyleLine(outputContent, "Default"));

                defaultStyle.FontName = subtitleSetting.GetFontName();
                defaultStyle.FontSize = subtitleSetting.FontSize;
                defaultStyle.Bold = subtitleSetting.Bold;
                defaultStyle.Italic = subtitleSetting.Italic;
                defaultStyle.Underline = subtitleSetting.Underline;
                defaultStyle.StrikeOut = subtitleSetting.Strikeout;
                defaultStyle.Outline = subtitleSetting.OutLine;

                beginEventIndex = outputContent.IndexOf(beginEventString, StringComparison.OrdinalIgnoreCase);

                outputContent = outputContent.Insert(beginEventIndex - 1, defaultStyle.ToString());
            }

            resultBuilder.AppendLine(outputContent);

            resultBuilder.Append(allDialogueFromSubtitles);

            await File.WriteAllTextAsync(filePath, resultBuilder.ToString());
        }
    }
}
