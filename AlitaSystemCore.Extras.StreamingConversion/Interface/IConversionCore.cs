namespace AlitaSystemCore.Extras.StreamingConversion.Interface;

public interface IConversionCore
{
    /// <summary>
    /// 转换Images为AVI视频
    /// </summary>
    /// <param name="imagePaths"></param>
    /// <param name="fps"></param>
    /// <returns>返回本地文件地址</returns>
    Task<string> EncodeImagesToAvi(List<string> imagePaths, int fps = 24);

    /// <summary>
    /// 转换Images为H264格式MP4视频
    /// </summary>
    /// <param name="imagePaths"></param>
    /// <param name="fps"></param>
    /// <returns>返回本地文件地址</returns>
    Task<string> EncodeImagesToH264ByFFmpeg(List<string> imagePaths, int fps = 24);
}