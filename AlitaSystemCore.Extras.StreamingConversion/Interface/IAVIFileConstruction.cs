namespace AlitaSystemCore.Extras.StreamingConversion.Interface;

public interface IAVIFileConstruction
{
    /// <summary>
    /// 构建AVI
    /// </summary>
    /// <param name="fileStream"></param>
    /// <param name="aviBuildParameter"></param>
    /// <returns></returns>
    Task Construction(FileStream fileStream, VideoBuildParameter aviBuildParameter);
}