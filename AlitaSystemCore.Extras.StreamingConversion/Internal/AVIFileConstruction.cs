namespace AlitaSystemCore.Extras.StreamingConversion.Internal;

public class AVIFileConstruction : IAVIFileConstruction
{
    #region 私有变量

    private int _nframes;        //总帧数
    private int _totalsize;      //帧的总大小
    private List<int> _sizeList; //保存各帧图像大小的链表，用于写索引块

    private byte[] _hdrl =
    {
        (byte)'h',
        (byte)'d',
        (byte)'r',
        (byte)'l'
    }; //hdrl

    private byte[] _list =
    {
        (byte)'L',
        (byte)'I',
        (byte)'S',
        (byte)'T'
    }; //LIST

    /*private byte[] _compression =
    {
        0x4D,
        0x4A,
        0x50,
        0x47
    }; //MJPG*/

    private byte[] _compression =
    {
        (byte)'J',
        (byte)'P',
        (byte)'E',
        (byte)'G'
    }; //JPEG

    private byte[] _riff =
    {
        (byte)'R',
        (byte)'I',
        (byte)'F',
        (byte)'F'
    }; //RIFF

    private byte[] _movi =
    {
        (byte)'m',
        (byte)'o',
        (byte)'v',
        (byte)'i'
    }; //movi

    private byte[] _tag =
    {
        (byte)'0',
        (byte)'0',
        (byte)'d',
        (byte)'c'
    }; //00dc = 压缩的视频数据

    private byte[] _avi =
    {
        (byte)'A',
        (byte)'V',
        (byte)'I',
        (byte)' '
    }; //AVI

    private byte[] _avih =
    {
        (byte)'a',
        (byte)'v',
        (byte)'i',
        (byte)'h'
    }; //avih

    private byte[] _idx1 =
    {
        (byte)'i',
        (byte)'d',
        (byte)'x',
        (byte)'1'
    }; //idx1

    private byte[] _strl =
    {
        (byte)'s',
        (byte)'t',
        (byte)'r',
        (byte)'l'
    }; //strl

    private byte[] _strh =
    {
        (byte)'s',
        (byte)'t',
        (byte)'r',
        (byte)'h'
    }; //strh 

    private byte[] _strf =
    {
        (byte)'s',
        (byte)'t',
        (byte)'r',
        (byte)'f'
    }; //strf 

    private byte[] _vids =
    {
        (byte)'v',
        (byte)'i',
        (byte)'d',
        (byte)'s'
    }; //流的类型，vids表示视频流，auds表示音频流

    private const ushort BitCount = 24;

    #endregion

    /// <summary>
    /// 写入索引
    /// </summary>
    /// <param name="fs"></param>
    private void WriteIndexChunk(FileStream fs)
    {
        var indexChunkSize = 16 * (uint)_nframes;

        // 写入Idxl
        fs.Write(_idx1, 0, 4);

        // 写入Size
        fs.Write(StructUtils.StructToBytes(indexChunkSize), 0, 4);

        uint keyframe = 0x10;
        uint offset   = 4;
        foreach (var node in _sizeList)
        {
            fs.Write(_tag, 0, 4);
            fs.Write(StructUtils.StructToBytes(keyframe), 0, 4);
            fs.Write(StructUtils.StructToBytes(offset), 0, 4);
            fs.Write(StructUtils.StructToBytes(node), 0, 4);

            offset += (uint)node + 8;
        }
    }

    /// <summary>
    /// 回填数据
    /// </summary>
    /// <param name="fp"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="fps"></param>
    /// <param name="imageSize"></param>
    private void FillData(FileStream fp, int width, int height, int fps, int imageSize)
    {
        var riffHead = new AVI_RIFF_HEAD
        {
            id = _riff,
            size = (uint)(4 + Marshal.SizeOf(typeof(AVI_HDRL_LIST)) + Marshal.SizeOf(typeof(AVI_LIST_HEAD)) +
                    _nframes * 8 + _totalsize),
            type = _avi
        };


        var hdrlList = new AVI_HDRL_LIST
        {
            id   = _list,
            size = (uint)(Marshal.SizeOf(typeof(AVI_HDRL_LIST)) - 8),
            type = _hdrl,
            avih = new AVI_AVIH_CHUNK
            {
                ChunkId             = _avih,
                ChunkSize           = (uint)(Marshal.SizeOf(typeof(AVI_AVIH_CHUNK)) - 8),
                MicroSecPerFrame    = (uint)(1.0 / fps * 1000),
                MaxBytesPerSec      = 25000,
                PaddingGranularity  = 0,
                Flags               = 0x10,
                TotalFrames         = (uint)_nframes,
                InitialFrames       = 0,
                Streams             = 1,
                SuggestedBufferSize = 100000,
                Width               = (uint)width,
                Height              = (uint)height,
                Reserved = new uint[]
                {
                    0x00,
                    0x00,
                    0x00,
                    0x00
                },
            },
            strl = new AVI_STRL_LIST
            {
                id   = _list,
                size = (uint)Marshal.SizeOf(typeof(AVI_STRL_LIST)) - 8,
                type = _strl,
                strh = new AVI_STRH_CHUNK
                {
                    Id                  = _strh,
                    Size                = (uint)Marshal.SizeOf(typeof(AVI_STRH_CHUNK)) - 8,
                    StreamType          = _vids,
                    Codec               = _compression,
                    Flags               = 0,
                    Priority            = 0,
                    Language            = 0,
                    InitialFrames       = 0,
                    Scale               = 1,
                    Rate                = (uint)fps,
                    Start               = 0,
                    Length              = (uint)_nframes,
                    SuggestedBufferSize = (uint)(imageSize * 10),
                    Quality             = 10000,
                    SampleSize          = 0,
                    RcFrame = new AVI_RECT_FRAME
                    {
                        left   = 0,
                        top    = 0,
                        right  = (short)width,
                        bottom = (short)height
                    }
                },
                strf = new AVI_STRF_CHUNK
                {
                    Id              = _strf,
                    Size            = (uint)Marshal.SizeOf(typeof(AVI_STRF_CHUNK)) - 8,
                    VideoSize       = (uint)Marshal.SizeOf(typeof(AVI_STRF_CHUNK)) - 8,
                    Width           = (uint)width,
                    Height          = (uint)height,
                    Planes          = 1,
                    BitCount        = BitCount,
                    Compression     = _compression,
                    SizeImage       = (uint)(width * height * BitCount / 8),
                    XPelsPerMeter   = 0,
                    YPelsPerMeter   = 0,
                    ColorsUsed      = 0,
                    ColorsImportant = 0
                }
            }
        };

        var moviListHead = new AVI_LIST_HEAD
        {
            Id   = _list,
            Size = (uint)(4 + _nframes * 8 + _totalsize),
            Type = _movi
        };

        //定位到文件头，回填各块数据
        fp.Seek(0, SeekOrigin.Begin);
        fp.Write(StructUtils.StructToBytes(riffHead));
        fp.Write(StructUtils.StructToBytes(hdrlList));
        fp.Write(StructUtils.StructToBytes(moviListHead));
    }

    /// <summary>
    /// 初始化Avi构建
    /// </summary>
    /// <param name="fp"></param>
    private void AviInit(FileStream fp)
    {
        var offset1 = Marshal.SizeOf(typeof(AVI_RIFF_HEAD)); //riff head大小
        var offset2 = Marshal.SizeOf(typeof(AVI_HDRL_LIST)); //hdrl list大小
        var offset3 = Marshal.SizeOf(typeof(AVI_LIST_HEAD)); //movi list head大小

        //AVI文件偏移量设置到movi list head后，从该位置向后依次写入JPEG数据
        fp.Seek(offset1 + offset2 + offset3, SeekOrigin.Begin);

        //初始化链表
        _sizeList = new List<int>();

        _nframes   = 0;
        _totalsize = 0;
    }

    /// <summary>
    /// 写索引以及回填数据
    /// </summary>
    /// <param name="fp"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="fps"></param>
    /// <param name="imageSize"></param>
    private void AviFinish(FileStream fp, int width, int height, int fps, int imageSize)
    {
        //写索引块
        WriteIndexChunk(fp);

        //从文件头开始，回填各块数据
        FillData(fp, width, height, fps, imageSize);
    }

    /// <summary>
    /// 填充图像
    /// </summary>
    /// <param name="fp"></param>
    /// <param name="data"></param>
    /// <param name="len"></param>
    private void AddFrame(FileStream fp, byte[] data, int len)
    {
        var tmp = _tag; //00dc = 压缩的视频数据

        var length = data.Length;

        fp.Write(tmp, 0, 4);                               // 写入是否是压缩的视频数据信息
        fp.Write(StructUtils.StructToBytes(length), 0, 4); // 写入4字节对齐后的JPEG图像大小
        fp.Write(data, 0, length);                         // 写入真正的JPEG数据

        _nframes   += 1;
        _totalsize += length;

        // 将4字节对齐后的JPEG图像大小保存在链表中
        _sizeList.Add(length);
    }

    /// <summary>
    /// 构建AVI
    /// </summary>
    /// <param name="fileStream"></param>
    /// <param name="aviBuildParameter"></param>
    /// <returns></returns>
    public async Task Construction(FileStream fileStream, VideoBuildParameter aviBuildParameter)
    {
        // 开始构建Avi文件
        AviInit(fileStream);

        var buffer = new byte[aviBuildParameter.ImageMaxLength];

        // 插入图片到视频中
        foreach (var imagePath in aviBuildParameter.ImagePaths)
        {
            await using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);

            _ = stream.Read(buffer, 0, aviBuildParameter.ImageMaxLength);

            // 将JPEG数据写入AVI文件
            AddFrame(fileStream, buffer, aviBuildParameter.ImageMaxLength);

            stream.Close();
        }

        // 写入索引完成构建Avi
        AviFinish(fileStream, aviBuildParameter.ImageSize.Width, aviBuildParameter.ImageSize.Height, 24,
                aviBuildParameter.ImageMaxLength);
    }
}