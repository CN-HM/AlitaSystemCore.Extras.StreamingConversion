namespace AlitaSystemCore.Extras.StreamingConversion.Extensions;

/// <summary>
/// 
/// </summary>
public sealed unsafe class VideoFrameConverter : IDisposable
{
    private readonly IntPtr _convertedFrameBufferPtr;
    private readonly Size _destinationSize;
    private readonly byte_ptr4 _dstData;
    private readonly int4 _dstLinesize;
    private readonly SwsContext* _pConvertContext;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceSize">源图像的宽度和高度</param>
    /// <param name="sourcePixelFormat">源图像的像素格式，例如AV_PIX_FMT_YUV420P</param>
    /// <param name="destinationSize">目标图像的宽度和高度</param>
    /// <param name="destinationPixelFormat">目标图像的像素格式</param>
    /// <exception cref="ApplicationException"></exception>
    public VideoFrameConverter(Size sourceSize, AVPixelFormat sourcePixelFormat, Size destinationSize,
                               AVPixelFormat destinationPixelFormat)
    {
        _destinationSize = destinationSize;

        _pConvertContext = ffmpeg.sws_getContext(sourceSize.Width, sourceSize.Height, sourcePixelFormat,
                destinationSize.Width, destinationSize.Height, destinationPixelFormat, ffmpeg.SWS_FAST_BILINEAR, null,
                null, null);

        if (_pConvertContext == null)
            throw new ApplicationException("Could not initialize the conversion context.");

        var convertedFrameBufferSize = ffmpeg.av_image_get_buffer_size(destinationPixelFormat,
                destinationSize.Width, destinationSize.Height, 1);

        _convertedFrameBufferPtr = Marshal.AllocHGlobal(convertedFrameBufferSize);
        _dstData                 = new byte_ptr4();
        _dstLinesize             = new int4();

        ffmpeg.av_image_fill_arrays(ref _dstData, ref _dstLinesize, (byte*)_convertedFrameBufferPtr,
                destinationPixelFormat, destinationSize.Width, destinationSize.Height, 1);
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        Marshal.FreeHGlobal(_convertedFrameBufferPtr);
        ffmpeg.sws_freeContext(_pConvertContext);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceFrame"></param>
    /// <returns></returns>
    public AVFrame Convert(AVFrame sourceFrame)
    {
        ffmpeg.sws_scale(_pConvertContext, sourceFrame.data, sourceFrame.linesize, 0, sourceFrame.height, _dstData,
                _dstLinesize);

        var data = new byte_ptr8();
        data.UpdateFrom(_dstData);
        var lineSize = new int8();
        lineSize.UpdateFrom(_dstLinesize);

        return new AVFrame
        {
            time_base = sourceFrame.time_base,
            pts       = sourceFrame.pts,
            data      = data,
            linesize  = lineSize,
            width     = _destinationSize.Width,
            height    = _destinationSize.Height
        };
    }
}