using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SuperEncode.Wpf.ViewModels;

namespace SuperEncode.Wpf.Services;

public class VideoService(SubtitleService subtitleService)
{
    private static readonly string BasePath = AppContext.BaseDirectory;

    public async Task<string> EncodeVideoWithNVencC(
        EncodeVideoInput input,
        SubtitleSetting subtitleSetting, VideoSetting encodeSetting,
        CancellationToken cancellation = default)
    {
        var subtitlePath = await subtitleService.GetSubtitleFromVideo(input.FilePath, subtitleSetting, cancellation);

        if (string.IsNullOrEmpty(subtitlePath)) return string.Empty;
        var inputFilePath = input.FilePath;

        var outputFilePath = Path.ChangeExtension(inputFilePath, ".mp4");

        var arguments =
            BuildNvEncCArguments(inputFilePath, outputFilePath, subtitlePath, subtitleSetting, encodeSetting);

        outputFilePath = await RunNvEncC(input, outputFilePath, arguments, cancellation);

        if (File.Exists(subtitlePath)) File.Delete(subtitlePath);
        return outputFilePath;
    }

    private static string BuildNvEncCArguments(
        string path, string outputFile, string subtitlePath,
        SubtitleSetting subtitleSetting, VideoSetting encodeSetting)
    {
        var builder = new StringBuilder();
        builder.Append(" --avsw --codec h264 ");
        builder.Append($"-i \"{path}\" ");

        builder.Append($"--vpp-subburn filename=\"{subtitlePath}\",charcode=utf-8 ");

        builder.Append($"--max-bitrate {subtitleSetting.MaxBitrate} ");

        //builder.Append($"--audio-copy 0 ");

        builder.Append("--audio-codec libmp3lame ");


        builder.Append("--log-level quiet,core_progress=info ");

        builder.Append(encodeSetting.EnableHdr ? "--vpp-colorspace hdr2sdr=hable " : "");

        builder.Append($" -o \"{outputFile}\" ");
        return builder.ToString();
    }

    public async Task<string> RunNvEncC(EncodeVideoInput input, string outputPath, string arguments,
        CancellationToken cancellationToken = default)
    {
        var nVencCPath = "NVEncC64.exe";

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
        App.RunningProcesses.Add(process);

        process.Start();

        try
        {
            while (!cancellationToken.IsCancellationRequested &&
                   await process.StandardError.ReadLineAsync(cancellationToken) is { } processOutput)
            {
                var regex = new Regex(@"\[(\d+\.\d+)%\]");

                var match = regex.Match(processOutput);

                if (!match.Success) continue;

                var percentageString = match.Groups[1].Value;
                var percentage = double.Parse(percentageString, new CultureInfo("en-US"));

                //Debug.WriteLine("Info: Process: " + percentage + "%");


                input.Percent = percentage;
            }

            await process.WaitForExitAsync(cancellationToken);
            App.RunningProcesses.Remove(process);
        }
        catch (OperationCanceledException)
        {
            input.Percent = 0;

            if (!process.HasExited)
            {
                process.Kill();
                App.RunningProcesses.Remove(process);

                await Task.Delay(100, cancellationToken).ConfigureAwait(false);

                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                    outputPath = "";
                }
            }
        }

        return outputPath;
    }
}