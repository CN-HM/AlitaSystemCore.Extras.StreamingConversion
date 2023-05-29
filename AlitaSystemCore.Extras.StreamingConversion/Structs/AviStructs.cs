namespace AlitaSystemCore.Extras.StreamingConversion.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AVI_RIFF_HEAD
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] id;

    public uint size;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] type;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AVI_LIST_HEAD
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Id;

    public uint Size;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Type;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AVI_AVIH_CHUNK
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] ChunkId; //块ID，固定为avih

    public uint ChunkSize; //块大小，等于struct avi_avih_chunk去掉id和size的大小

    public uint MicroSecPerFrame; //视频帧间隔时间(以微秒为单位)

    public uint MaxBytesPerSec; //AVI文件的最大数据率

    public uint PaddingGranularity; //设为0即可

    public uint Flags; //AVI文件全局属性，如是否含有索引块、音视频数据是否交叉存储等

    public uint TotalFrames; //总帧数

    public uint InitialFrames; //为交互格式指定初始帧数(非交互格式应该指定为0)

    public uint Streams; //文件包含的流的个数，仅有视频流时为1

    public uint SuggestedBufferSize; //指定读取本文件建议使用的缓冲区大小，通常为存储一桢图像  

    public uint Width; //视频主窗口宽度（单位：像素）

    public uint Height; //视频主窗口高度（单位：像素）

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public uint[] Reserved; //保留段，设为0即可
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AVI_RECT_FRAME
{
    public short left;

    public short top;

    public short right;

    public short bottom;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AVI_STRH_CHUNK
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Id; //块ID，固定为strh

    public uint Size; //块大小，等于struct avi_strh_chunk去掉id和size的大小

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] StreamType; //流的类型，vids表示视频流，auds表示音频流

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Codec; //指定处理这个流需要的解码器，如JPEG

    public uint Flags; //标记，如是否允许这个流输出、调色板是否变化等，一般设为0即可

    public ushort Priority; //流的优先级，视频流设为0即可

    public ushort Language; //音频语言代号，视频流设为0即可

    public uint InitialFrames; //为交互格式指定初始帧数(非交互格式应该指定为0)

    public uint Scale;

    public uint Rate; //对于视频流，rate / scale = 帧率fps

    public uint Start; //对于视频流，设为0即可

    public uint Length; //对于视频流，length即总帧数

    public uint SuggestedBufferSize; //读取这个流数据建议使用的缓冲区大小

    public uint Quality; //流数据的质量指标

    public uint SampleSize; //音频采样大小，视频流设为0即可

    public AVI_RECT_FRAME RcFrame; //这个流在视频主窗口中的显示位置，设为{0,0，width,height}即可
}

/*对于视频流，strf块结构如下*/
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AVI_STRF_CHUNK
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Id; // 块ID，固定为strf

    public uint Size;      // 块大小，等于struct avi_strf_chunk去掉id和size的大小
    public uint VideoSize; // size1含义和值同size一样

    public uint Width;  // 视频主窗口宽度（单位：像素）
    public uint Height; // 视频主窗口高度（单位：像素）

    public ushort Planes;   // 始终为1
    public ushort BitCount; // 每个像素占的位数，只能是1、4、8、16、24和32中的一个

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Compression; // 视频流编码格式，如JPEG、MJPG等

    public uint SizeImage; // 视频图像大小，等于width * height * bitcount / 8

    public uint XPelsPerMeter; // 显示设备的水平分辨率，设为0即可
    public uint YPelsPerMeter; // 显示设备的垂直分辨率，设为0即可

    public uint ColorsUsed;      // 含义不清楚，设为0即可
    public uint ColorsImportant; // 含义不清楚，设为0即可
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AVI_STRL_LIST
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] id; // 块ID，固定为LIST

    public uint size; // 块大小，等于struct avi_strl_list去掉id和size的大小

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] type; // 块类型，固定为strl

    public AVI_STRH_CHUNK strh;
    public AVI_STRF_CHUNK strf;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AVI_HDRL_LIST
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] id; // 块ID，固定为LIST

    public uint size; // 块大小，等于struct avi_hdrl_list去掉id和size的大小

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] type; // 块类型，固定为hdrl

    public AVI_AVIH_CHUNK avih;
    public AVI_STRL_LIST strl;
}