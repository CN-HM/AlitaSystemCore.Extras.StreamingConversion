namespace AlitaSystemCore.Extras.StreamingConversion.Internal;

/// <summary>
/// 生成视频 转换模型
/// </summary>
public class ConvertVideoBuildModel
{
    /// <summary>
    /// 
    /// </summary>
    public string OutputFilePath { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public int Fps { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public List<Image> Images { get; set; }
}
