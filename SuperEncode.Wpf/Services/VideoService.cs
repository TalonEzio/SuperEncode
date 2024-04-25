using System.Diagnostics;
using System.IO;
using System.Text;
using SuperEncode.Wpf.ViewModels;

namespace SuperEncode.Wpf.Services
{
    public class VideoService(SubtitleService subtitleService)
    {
        private static readonly string BasePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()!.Location)!;
        public async Task<string> EncodeVideoWithNVencC(
            string inputFile, SubtitleSetting subtitleSetting, VideoSetting encodeSetting)
        {
            var subtitlePath = await subtitleService.ConvertToAss(inputFile,subtitleSetting,encodeSetting);
            var outputFile = Path.ChangeExtension(inputFile, ".mp4");

            var arguments = BuildNvEncCArguments(inputFile, outputFile, subtitlePath,subtitleSetting,encodeSetting);

            await RunNvEncC(arguments, encodeSetting);

            File.Delete(subtitlePath);
            return outputFile;
        }
        
        private  string BuildNvEncCArguments(string inputFile, string outputFile, string subtitlePath,SubtitleSetting subtitleSetting,VideoSetting encodeSetting)
        {
            var builder = new StringBuilder();
            builder.Append($" --avsw --codec h264 -i \"{inputFile}\" ");
            builder.Append($"--vpp-subburn filename=\"{subtitlePath}\",charcode=utf-8 ");
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
