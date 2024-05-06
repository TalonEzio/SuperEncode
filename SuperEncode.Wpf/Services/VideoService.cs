using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SuperEncode.Wpf.Extensions;
using SuperEncode.Wpf.ViewModels;
using Xabe.FFmpeg;

namespace SuperEncode.Wpf.Services
{
    public class VideoService(SubtitleService subtitleService)
    {
        private event EventHandler<VideoProcessEventArgs>? VideoProcessEventHandler;

        public event EventHandler<VideoProcessEventArgs> VideoEventHandler
        {
            add => VideoProcessEventHandler += value;
            remove => VideoProcessEventHandler -= value;
        }
        private static readonly string BasePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()!.Location)!;
        public async Task<string> EncodeVideoWithNVencC(
            IMediaInfo fileInfo, SubtitleSetting subtitleSetting, VideoSetting encodeSetting)
        {
            var subtitlePath = await subtitleService.ConvertToAss(fileInfo, subtitleSetting);

            var inputFile = fileInfo.Path;

            var outputFile = Path.ChangeExtension(inputFile, ".mp4");

            var arguments = BuildNvEncCArguments(fileInfo, outputFile, subtitlePath, subtitleSetting, encodeSetting);

            await RunNvEncC(arguments);

            File.Delete(subtitlePath);
            return outputFile;
        }

        private static string BuildNvEncCArguments(IMediaInfo fileInfo, string outputFile, string subtitlePath, SubtitleSetting subtitleSetting, VideoSetting encodeSetting)
        {
            var builder = new StringBuilder();
            builder.Append($" --avsw --codec h264 ");
            builder.Append($"-i \"{fileInfo.Path}\" ");

            var videoStream = fileInfo.VideoStreams.First();
            builder.Append($"--vpp-subburn filename=\"{subtitlePath}\",charcode=utf-8,scale={videoStream.Width * 1.0 / 1920} ");

            builder.Append($"--max-bitrate {subtitleSetting.MaxBitrate} ");

            builder.Append("--audio-codec libmp3lame ");
            builder.Append("--log-level quiet,core_progress=info ");

            builder.Append(encodeSetting.EnableHdr ? "--vpp-colorspace hdr2sdr=hable " : "");

            builder.Append($" -o \"{outputFile}\" ");
            return builder.ToString();
        }


        public async Task RunNvEncC(string arguments)
        {
            var nVencCPath = Path.Combine(BasePath, "Tools", "NVEncC", "NVEncC.exe");

            var startInfo = new ProcessStartInfo
            {
                FileName = nVencCPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true, 
                RedirectStandardError = true
            };

            using var process = new Process();
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.Start();

            while (await process.StandardError.ReadLineAsync() is { } processOutput)
            {
                var regex = new Regex(@"\[(\d+\.\d+)%\]");

                var match = regex.Match(processOutput);

                if (!match.Success) continue;

                var percentageString = match.Groups[1].Value;
                var percentage = double.Parse(percentageString);

                Debug.WriteLine("Info: Process: " + percentage + "%");

                var callbackValue = new VideoProcessEventArgs()
                {
                    Percentage = percentage
                };

                VideoProcessEventHandler?.Invoke(process, callbackValue);
            }
            await process.WaitForExitAsync();
        }
    }
}
