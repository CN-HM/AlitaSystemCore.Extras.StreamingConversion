namespace AlitaSystemCore.Extras.StreamingConversion.Extensions;

internal static class FFmpegHelper
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="error"></param>
    /// <returns></returns>
    public static unsafe string AvStrerror(int error)
    {
        var bufferSize = 1024;
        var buffer     = stackalloc byte[bufferSize];
        ffmpeg.av_strerror(error, buffer, (ulong)bufferSize);
        var message = Marshal.PtrToStringAnsi((IntPtr)buffer);
        return message;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="error"></param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    public static int ThrowExceptionIfError(this int error)
    {
        if (error < 0)
            throw new ApplicationException(AvStrerror(error));
        return error;
    }

    /// <summary>
    /// 注册环境
    /// </summary>
    public static void RegisterFFmpegBinaries(string filePath)
    {
        var current = Environment.CurrentDirectory;
        var probe   = Path.Combine(filePath, "bin", "x64");

        while (current != null)
        {
            var ffmpegBinaryPath = Path.Combine(current, probe);

            if (Directory.Exists(ffmpegBinaryPath))
            {
                Console.WriteLine($"FFmpeg binaries found in: {ffmpegBinaryPath}");
                DynamicallyLoadedBindings.LibrariesPath = ffmpegBinaryPath;
                return;
            }

            current = Directory.GetParent(current)
                    ?.FullName;
        }
    }
}