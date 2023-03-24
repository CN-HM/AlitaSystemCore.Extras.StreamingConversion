namespace AlitaSystemCore.Extras.StreamingConversion.Models;

/// <summary>
/// 构建AVI参数
/// </summary>
public class AviBuildParameter
{
    /// <summary>
    /// 图片最大对齐4字节长度
    /// </summary>
    /// <remarks>一般取首张图片作为图片最大大小</remarks>
    public int ImageMaxLength { get; set; }

    /// <summary>
    /// 图片路径
    /// </summary>
    public List<string> ImagePaths { get; set; }

    /// <summary>
    /// 视频固定高度 
    /// </summary>
    /// <remarks>一般取首张图片高度</remarks>
    public int FixedHeight { get; set; }

    /// <summary>
    /// 视频固定宽度
    /// </summary>
    /// <remarks>一般取首张图片宽度</remarks>
    public int FixedWidth { get; set; }
}