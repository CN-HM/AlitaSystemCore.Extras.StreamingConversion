namespace AlitaSystemCore.Extras.StreamingConversion.Extensions;

/// <summary>
/// 
/// </summary>
public sealed unsafe class ImageToMp4Conversion : IDisposable
{
    private readonly VideoBuildParameter _videoBuildParameter;
    private readonly AVStream* _pStream;
    private readonly AVCodecContext* _pCodecContext;
    private readonly AVFormatContext* _pFormatContext;
    private readonly AVCodecID _codecId = AVCodecID.AV_CODEC_ID_H264;
    private readonly AVPixelFormat _sourcePixelFormat = AVPixelFormat.AV_PIX_FMT_BGR24;
    private readonly AVPixelFormat _destinationPixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;
    private readonly VideoFrameConverter _videoFrameConverter;
    private int _frameNumber;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ImageToMp4Conversion(VideoBuildParameter videoBuildParameter)
    {
        _frameNumber         = 0;
        _videoBuildParameter = videoBuildParameter;
        _pFormatContext      = ffmpeg.avformat_alloc_context();

        var pFormatContext = _pFormatContext;
        ffmpeg.avformat_alloc_output_context2(&pFormatContext, null, "mp4", videoBuildParameter.VideoFilePath)
                .ThrowExceptionIfError();

        // 设置视频流参数
        var pCodec    = ffmpeg.avcodec_find_encoder(_codecId);
        var outFormat = ffmpeg.av_guess_format("mp4", null, null);
        _pFormatContext->oformat = outFormat;

        // 设置编码参数
        _pStream = ffmpeg.avformat_new_stream(_pFormatContext, pCodec);
        _pStream->time_base = new AVRational
        {
            num = 1,
            den = _videoBuildParameter.Fps
        };

        _pCodecContext         = ffmpeg.avcodec_alloc_context3(pCodec);
        _pCodecContext->width  = _videoBuildParameter.ImageSize.Width;
        _pCodecContext->height = _videoBuildParameter.ImageSize.Height;
        _pCodecContext->time_base = new AVRational
        {
            num = 1,
            den = _videoBuildParameter.Fps
        };
        _pCodecContext->codec_type    =  AVMediaType.AVMEDIA_TYPE_VIDEO;
        _pCodecContext->has_b_frames  =  0;
        _pCodecContext->max_b_frames  =  0;
        _pCodecContext->framerate.num =  _videoBuildParameter.Fps; // 帧率
        _pCodecContext->framerate.den =  1;
        _pCodecContext->gop_size      =  _videoBuildParameter.Fps; // 每秒1个关键帧
        _pCodecContext->has_b_frames  =  0;
        _pCodecContext->max_b_frames  =  0;
        _pCodecContext->flags         |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
        _pCodecContext->pix_fmt       =  AVPixelFormat.AV_PIX_FMT_YUV420P; // 像素格式
        _pCodecContext->sample_rate   =  0;                                // 将sample_rate设置为0，表示不需要处理音频

        ffmpeg.av_opt_set(_pCodecContext->priv_data, "preset", "veryslow", 0);

        // 打开音视频编码器
        ffmpeg.avcodec_open2(_pCodecContext, pCodec, null)
                .ThrowExceptionIfError();

        ffmpeg.avcodec_parameters_from_context(_pStream->codecpar, _pCodecContext);

        // 写入文件头
        if ((_pFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
        {
            // 打开输出文件
            ffmpeg.avio_open(&_pFormatContext->pb, _videoBuildParameter.VideoFilePath, ffmpeg.AVIO_FLAG_WRITE)
                    .ThrowExceptionIfError();
        }

        ffmpeg.avformat_write_header(_pFormatContext, null)
                .ThrowExceptionIfError();

        _videoFrameConverter = new VideoFrameConverter(_videoBuildParameter.ImageSize, _sourcePixelFormat,
                _videoBuildParameter.ImageSize, _destinationPixelFormat);
    }

    /// <summary>
    /// 构建视频
    /// </summary>
    public void Build()
    {
        foreach (var path in _videoBuildParameter.ImagePaths)
        {
            byte[] bitmapData;

            using (var frameImage = Image.FromFile(path))
                using (var frameBitmap = frameImage as Bitmap ?? new Bitmap(frameImage))
                    bitmapData = GetBitmapData(frameBitmap);

            fixed (byte* pBitmapData = bitmapData)
            {
                // 单帧
                var frame = new AVFrame
                {
                    pts = _frameNumber * _pCodecContext->time_base.den / _pCodecContext->time_base.num,
                    time_base = new AVRational
                    {
                        num = 1,
                        den = _videoBuildParameter.Fps
                    },
                    data = new byte_ptr8
                    {
                        [0] = pBitmapData
                    },
                    linesize = new int8
                    {
                        [0] = bitmapData.Length / _videoBuildParameter.ImageSize.Height
                    },
                    height = _videoBuildParameter.ImageSize.Height
                };
                var convertedFrame = _videoFrameConverter.Convert(frame);
                Encode(convertedFrame, _frameNumber);
            }

            Console.WriteLine($"frame: {_frameNumber}");
            _frameNumber++;
        }

        Drain();
    }

    /// <summary>
    /// 
    /// </summary>
    public void Encode(AVFrame frame, int frameNumber)
    {
        var pPacket = ffmpeg.av_packet_alloc();

        try
        {
            ffmpeg.avcodec_send_frame(_pCodecContext, &frame)
                    .ThrowExceptionIfError();

            bool hasFinishedWithThisFrame;

            do
            {
                ffmpeg.av_packet_unref(pPacket);

                var response = ffmpeg.avcodec_receive_packet(_pCodecContext, pPacket);

                bool isPacketValid;

                if (response == 0)
                {
                    isPacketValid            = true;
                    hasFinishedWithThisFrame = false;
                }
                else if (response == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                {
                    isPacketValid            = false;
                    hasFinishedWithThisFrame = true;
                }
                else if (response == ffmpeg.AVERROR(ffmpeg.AVERROR_EOF))
                {
                    isPacketValid            = false;
                    hasFinishedWithThisFrame = true;
                }
                else
                {
                    throw new InvalidOperationException($"error from avcodec_receive_packet: {response}");
                }

                if (!isPacketValid)
                    continue;

                ffmpeg.av_packet_rescale_ts(pPacket, _pCodecContext->time_base, _pStream->time_base);
                ffmpeg.av_interleaved_write_frame(_pFormatContext, pPacket);
            } while (!hasFinishedWithThisFrame);
        }
        finally
        {
            ffmpeg.av_packet_unref(pPacket);
            ffmpeg.av_packet_free(&pPacket);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void Drain()
    {
        var pPacket = ffmpeg.av_packet_alloc();

        try
        {
            ffmpeg.avcodec_send_frame(_pCodecContext, null)
                    .ThrowExceptionIfError();

            bool hasFinishedDraining;

            do
            {
                ffmpeg.av_packet_unref(pPacket);

                var response = ffmpeg.avcodec_receive_packet(_pCodecContext, pPacket);

                bool isPacketValid;

                if (response == 0)
                {
                    isPacketValid       = true;
                    hasFinishedDraining = false;
                }
                else if (response == ffmpeg.AVERROR(ffmpeg.AVERROR_EOF))
                {
                    isPacketValid       = false;
                    hasFinishedDraining = true;
                }
                else
                {
                    isPacketValid       = false;
                    hasFinishedDraining = true;
                }

                if (!isPacketValid)
                    continue;

                ffmpeg.av_packet_rescale_ts(pPacket, _pCodecContext->time_base, _pStream->time_base);
                ffmpeg.av_interleaved_write_frame(_pFormatContext, pPacket);
            } while (!hasFinishedDraining);
        }
        finally
        {
            ffmpeg.av_packet_unref(pPacket);
            ffmpeg.av_packet_free(&pPacket);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="frameBitmap"></param>
    /// <returns></returns>
    public byte[] GetBitmapData(Bitmap frameBitmap)
    {
        var bitmapData = frameBitmap.LockBits(new Rectangle(Point.Empty, frameBitmap.Size), ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

        try
        {
            var length = bitmapData.Stride * bitmapData.Height;
            var data   = new byte[length];
            Marshal.Copy(bitmapData.Scan0, data, 0, length);
            return data;
        }
        finally
        {
            frameBitmap.UnlockBits(bitmapData);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        _videoFrameConverter.Dispose();

        var pFormatContext = _pFormatContext;
        ffmpeg.av_write_trailer(pFormatContext)
                .ThrowExceptionIfError();

        // 1、释放avcodec_free_context
        var pCodecContext = _pCodecContext;
        ffmpeg.avcodec_free_context(&pCodecContext);

        // 2、释放pFormatContext
        if ((pFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
        {
            ffmpeg.avio_closep(&pFormatContext->pb);
        }

        ffmpeg.avformat_free_context(pFormatContext);
    }
}