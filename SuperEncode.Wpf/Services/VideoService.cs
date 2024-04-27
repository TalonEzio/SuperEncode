using System.Diagnostics;
using System.IO;
using System.Text;
using SuperEncode.Wpf.ViewModels;
using Xabe.FFmpeg;

namespace SuperEncode.Wpf.Services
{
    public class VideoService(SubtitleService subtitleService)
    {
        private static readonly string BasePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()!.Location)!;
        public async Task<string> EncodeVideoWithNVencC(
            IMediaInfo fileInfo, SubtitleSetting subtitleSetting, VideoSetting encodeSetting)
        {
            var subtitlePath = await subtitleService.ConvertToAss(fileInfo,subtitleSetting,encodeSetting);

            var inputFile = fileInfo.Path;

            var outputFile = Path.ChangeExtension(inputFile, ".mp4");

            var arguments = BuildNvEncCArguments(fileInfo, outputFile, subtitlePath,subtitleSetting,encodeSetting);

            await RunNvEncC(arguments, encodeSetting);

            File.Delete(subtitlePath);
            return outputFile;
        }
        
        private  string BuildNvEncCArguments(IMediaInfo fileInfo, string outputFile, string subtitlePath,SubtitleSetting subtitleSetting,VideoSetting encodeSetting)
        {
            var builder = new StringBuilder();
            builder.Append($" --avsw --codec h264 -i \"{fileInfo.Path}\" ");

            var videoStream = fileInfo.VideoStreams.First();
            builder.Append($"--vpp-subburn filename=\"{subtitlePath}\",charcode=utf-8,scale={videoStream.Width * 1.0 / 1920} ");

            builder.Append($"--max-bitrate {subtitleSetting.MaxBitrate} ");

            builder.Append("--audio-codec libmp3lame ");

            builder.Append(encodeSetting.EnableHdr ? "--vpp-colorspace hdr2sdr=hable " : "");

            builder.Append($"-o \"{outputFile}\" ");
            return builder.ToString();
        }

        public  async Task RunNvEncC(string arguments, VideoSetting encodeSetting)
        {
            var nVencCPath = Path.Combine(BasePath, "Tools", "NVEncC", "NVEncC.exe");
            var startInfo = new ProcessStartInfo
            {
                FileName = nVencCPath,
                Arguments = arguments,
                UseShellExecute = encodeSetting.EnableCmd,
                CreateNoWindow = !encodeSetting.EnableCmd
            };

            using var process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            await process.WaitForExitAsync();
        }
        
    }
}
