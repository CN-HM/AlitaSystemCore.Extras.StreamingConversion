using AlitaSystemCore.Extras.StreamingConversion.Internal;

namespace AlitaSystemCore.Extras.StreamingConversion.Interface;

public interface IConvertVideoServices
{
    /// <summary>
    /// 构建视频
    /// </summary>
    /// <param name="images"></param>
    /// <param name="fps"></param>
    /// <returns></returns>
    Task<string> BuildVideo(List<Image> images, int fps = 24);
}
