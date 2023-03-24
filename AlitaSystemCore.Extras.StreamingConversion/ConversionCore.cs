using System.Drawing;
using AlitaSystemCore.Extras.StreamingConversion.Interface;
using AlitaSystemCore.Extras.StreamingConversion.Models;

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
    /// 转换Images为AVI视频，
    /// </summary>
    /// <returns>返回本地文件地址</returns>
    /// <param name="imagePaths"></param>
    /// <returns></returns>
    public async Task<string> ConversionToAvi(List<string> imagePaths)
    {
        // 判空
        if (imagePaths.Count <= 0)
            throw new ArgumentNullException(nameof(imagePaths),
                    "The passing parameter of the image collection cannot be 0.");

        // 获取第一张图片大小
        await using var fileStream = new FileStream(imagePaths.First(), FileMode.Open, FileAccess.Read);

        var firstImage = Image.FromStream(fileStream);

        var width  = firstImage.Width;
        var height = firstImage.Height;

        var length = (int)fileStream.Length; // 获取文件长度

        if (length <= 0)
            throw new InvalidOperationException();

        while (length % 4 != 0)
        {
            length++;
        }

        fileStream.Close();

        // 组合文件路径
        var videoFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                $"{TempVideoFilesPath}{TempVideoFilesFirstName}{Guid.NewGuid():N}.avi");

        // 生成视频文件
        await using var fpAvi = new FileStream(videoFilePath, FileMode.Create);

        // 构建Avi文件
        await _jpegToAvi.Construction(fpAvi, new AviBuildParameter
        {
            FixedWidth      = width,
            FixedHeight     = height,
            ImageMaxLength     = length,
            ImagePaths = imagePaths
        });

        fpAvi.Close();

        return videoFilePath;
    }
}