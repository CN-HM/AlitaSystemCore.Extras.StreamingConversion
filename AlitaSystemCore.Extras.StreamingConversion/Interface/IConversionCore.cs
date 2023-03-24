namespace AlitaSystemCore.Extras.StreamingConversion.Interface;

public interface IConversionCore
{
    /// <summary>
    /// 转换Images为AVI视频，
    /// </summary>
    /// <returns>返回本地文件地址</returns>
    /// <param name="imagePaths"></param>
    /// <returns></returns>
    Task<string> ConversionToAvi(List<string> imagePaths);
}