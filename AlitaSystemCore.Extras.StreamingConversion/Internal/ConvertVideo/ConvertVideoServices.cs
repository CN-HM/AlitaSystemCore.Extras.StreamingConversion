namespace AlitaSystemCore.Extras.StreamingConversion.Internal.ConvertVideo;

/// <summary>
/// 转换视频实现
/// TODO：
/// </summary>
public class ConvertVideoServices : IConvertVideoServices
{
    private const string TempVideoFilesPath      = @"TempVideoFiles\";
    private const string TempVideoFilesFirstName = "ConversionCore_";
    
    /// <summary>
    /// 构建视频
    /// </summary>
    public Task<string> BuildVideo(List<Image> images, int fps = 24)
    {
        // 判空
        if (images.Count <= 0)
            throw new ArgumentNullException(nameof(images),
                "The passing parameter of the image collection cannot be 0.");

        // 组合文件路径
        var videoFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            $"{TempVideoFilesPath}{TempVideoFilesFirstName}{Guid.NewGuid():N}.mp4");

        // 生成视频
        using var handle = new ConvertVideoHandle(new ConvertVideoBuildModel
        {
            OutputFilePath = videoFilePath,
            Fps = fps,
            Images = images
        });
        
        handle.BuildVideo();

        return Task.FromResult(videoFilePath);
    }
}
