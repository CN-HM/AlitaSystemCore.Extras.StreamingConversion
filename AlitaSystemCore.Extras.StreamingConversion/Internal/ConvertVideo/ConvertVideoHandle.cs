using AlitaSystemCore.Extras.StreamingConversion.Extensions;

namespace AlitaSystemCore.Extras.StreamingConversion.Internal.ConvertVideo;

public class ConvertVideoHandle : IDisposable
{
    private unsafe AVCodecContext* _pCodecContext;
    private unsafe AVFormatContext* _pFormatContext;
    private unsafe AVStream* _pStream;
    private unsafe AVFrame* _currentFrame;
    private        AVPixelFormat _sourcePixelFormat = AVPixelFormat.AV_PIX_FMT_BGR24; //.AV_PIX_FMT_BGR24;
    private        AVPixelFormat _destinationPixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P; //AV_PIX_FMT_YUV420P;
    private        ConvertVideoBuildModel _convertVideoBuildModel;
    private        VideoFrameConverter _videoFrameConverter = null!;
    private        int _width;
    private        int _height;
        
    /// <summary>
    /// 构造函数
    /// </summary>
    public ConvertVideoHandle(ConvertVideoBuildModel convertVideoBuildModel)
    {
        _convertVideoBuildModel = convertVideoBuildModel;

        var image = _convertVideoBuildModel.Images.First();
        _width  = image.Width;
        _height = image.Height;
    }

    public void BuildVideo()
    {
        unsafe
        {
            _pFormatContext = ffmpeg.avformat_alloc_context();

            var pFormatContext = _pFormatContext;
            ffmpeg.avformat_alloc_output_context2(&pFormatContext, null, "mp4", _convertVideoBuildModel.OutputFilePath)
                .ThrowExceptionIfError();

            // 设置视频流参数
            var pCodec = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_H264); // AV_CODEC_ID_MPEG4 AV_CODEC_ID_H264
            var outFormat = ffmpeg.av_guess_format("mp4", null, null);
            _pFormatContext->oformat = outFormat;

            // 设置编码参数
            _pStream = ffmpeg.avformat_new_stream(_pFormatContext, pCodec);
            _pStream->time_base = new AVRational
            {
                num = 1,
                den = _convertVideoBuildModel.Fps
            };

            _pCodecContext = ffmpeg.avcodec_alloc_context3(pCodec);

            //_pCodecContext->bit_rate     = 900000;
            _pCodecContext->width  = _width;
            _pCodecContext->height = _height;
            _pCodecContext->time_base = new AVRational
            {
                num = 1,
                den = _convertVideoBuildModel.Fps
            };
            _pCodecContext->codec_type    =  AVMediaType.AVMEDIA_TYPE_VIDEO;
            _pCodecContext->has_b_frames  =  0;
            _pCodecContext->max_b_frames  =  0;
            _pCodecContext->framerate.num =  _convertVideoBuildModel.Fps; // 帧率
            _pCodecContext->framerate.den =  1;
            _pCodecContext->gop_size      =  _convertVideoBuildModel.Fps; // 每秒1个关键帧
            _pCodecContext->has_b_frames  =  0;
            _pCodecContext->max_b_frames  =  0;
            _pCodecContext->flags         |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
            _pCodecContext->pix_fmt       =  AVPixelFormat.AV_PIX_FMT_YUV420P; // 像素格式
            _pCodecContext->sample_rate   =  0;                                // 将sample_rate设置为0，表示不需要处理音频

            ffmpeg.av_opt_set(_pCodecContext->priv_data, "preset", "veryslow", 0); // veryslow

            // 打开音视频编码器
            ffmpeg.avcodec_open2(_pCodecContext, pCodec, null)
                .ThrowExceptionIfError();

            ffmpeg.avcodec_parameters_from_context(_pStream->codecpar, _pCodecContext);

            // 写入文件头
            if ((_pFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
            {
                // 打开输出文件
                ffmpeg.avio_open(&_pFormatContext->pb, _convertVideoBuildModel.OutputFilePath, ffmpeg.AVIO_FLAG_WRITE)
                    .ThrowExceptionIfError();
            }

            ffmpeg.avformat_write_header(_pFormatContext, null)
                .ThrowExceptionIfError();

            var firstFrameImage = _convertVideoBuildModel.Images.First();
            var destinationSize = firstFrameImage.Size;

            _videoFrameConverter = new VideoFrameConverter(firstFrameImage.Size,
                _sourcePixelFormat, destinationSize,
                _destinationPixelFormat);

            var frameNumber = 0;
            foreach (var image in _convertVideoBuildModel.Images)
            {
                byte[] bitmapData;
                
                using (var frameBitmap = image as Bitmap ?? new Bitmap(image))
                    bitmapData = GetBitmapData(frameBitmap);
                
                fixed (byte* pBitmapData = bitmapData)
                {
                    // 单帧
                    var frame = new AVFrame
                    {
                        pts = frameNumber * 1, //_pCodecContext->time_base.den / _pCodecContext->time_base.num,
                        time_base = new AVRational
                        {
                            num = 1,
                            den = _convertVideoBuildModel.Fps
                        },
                        data = new byte_ptr8
                        {
                            [0] = pBitmapData
                        },
                        linesize = new int8
                        {
                            [0] = bitmapData.Length / _height
                        },
                        height = _height
                    };
                    var fm = _videoFrameConverter.Convert(frame);
                    _currentFrame = &fm;
                    Encode();
                }

                frameNumber++;
            }

            Drain();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private void Encode()
    {
        unsafe
        {
            var pPacket = ffmpeg.av_packet_alloc();

            try
            {
                ffmpeg.avcodec_send_frame(_pCodecContext, _currentFrame)
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
    }

    /// <summary>
    /// 
    /// </summary>
    private void Drain()
    {
        unsafe
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
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="frameBitmap"></param>
    /// <returns></returns>
    private byte[] GetBitmapData(Bitmap frameBitmap)
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
        unsafe
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
}
