using AlitaSystemCore.Extras.StreamingConversion.Extensions;

namespace AlitaSystemCore.Extras.StreamingConversion;

/// <summary>
/// 流媒体转换核心方法
/// </summary>
public class ConversionCore : IConversionCore
{
    private const string TempVideoFilesPath = @"TempVideoFiles\";
    private const string TempVideoFilesFirstName = "ConversionCore_";
    private readonly IAVIFileConstruction _jpegToAvi;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConversionCore(IAVIFileConstruction jpegToAvi)
    {
        _jpegToAvi = jpegToAvi;
    }

    /// <summary>
    /// 转换Images为AVI视频
    /// </summary>
    /// <returns>返回本地文件地址</returns>
    /// <param name="imagePaths"></param>
    /// <returns></returns>
    public async Task<string> EncodeImagesToAvi(List<string> imagePaths, int fps = 24)
    {
        // 判空
        if (imagePaths.Count <= 0)
            throw new ArgumentNullException(nameof(imagePaths),
                    "The passing parameter of the image collection cannot be 0.");

        // 获取第一张图片大小
        await using var fileStream = new FileStream(imagePaths.First(), FileMode.Open, FileAccess.Read);

        var firstImage = Image.FromStream(fileStream);

        var length = (int)fileStream.Length; // 获取文件长度

        fileStream.Close();

        if (length <= 0)
            throw new InvalidOperationException();

        // 对齐
        while (length % 4 != 0)
        {
            length++;
        }

        // 组合文件路径
        var videoFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                $"{TempVideoFilesPath}{TempVideoFilesFirstName}{Guid.NewGuid():N}.avi");

        // 生成视频文件
        await using var fpAvi = new FileStream(videoFilePath, FileMode.Create);

        // 构建Avi文件
        await _jpegToAvi.Construction(fpAvi, new VideoBuildParameter
        {
            ImageMaxLength = (int)fileStream.Length,
            ImagePaths     = imagePaths,
            ImageSize = firstImage.Size,
            Fps = fps
        });

        // 释放文件
        fpAvi.Close();

        return videoFilePath;
    }

    /// <summary>
    /// 转换Images为H264格式MP4视频
    /// </summary>
    /// <param name="imagePaths"></param>
    /// <param name="fps"></param>
    /// <returns>返回本地文件地址</returns>
    public async Task<string> EncodeImagesToH264ByFFmpeg(List<string> imagePaths, int fps = 24)
    {
        // 判空
        if (imagePaths.Count <= 0)
            throw new ArgumentNullException(nameof(imagePaths),
                    "The passing parameter of the image collection cannot be 0.");

        // 获取第一张图片大小
        await using var fileStream = new FileStream(imagePaths.First(), FileMode.Open, FileAccess.Read);

        var firstImage = Image.FromStream(fileStream);

        fileStream.Close();

        // 组合文件路径
        var videoFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                $"{TempVideoFilesPath}{TempVideoFilesFirstName}{Guid.NewGuid():N}.mp4");

        // 初始化转换
        var imageToMp4Conversion = new ImageToMp4Conversion(new VideoBuildParameter
        {
            VideoFilePath = videoFilePath,
            ImageSize  = firstImage.Size,
            ImagePaths = imagePaths,
            Fps        = fps
        });

        // 生成视频
        imageToMp4Conversion.Build();

        // 释放资源
        imageToMp4Conversion.Dispose();

        return videoFilePath;
    }
}