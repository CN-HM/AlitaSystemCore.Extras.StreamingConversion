using AlitaSystemCore.Extras.StreamingConversion.Extensions;

namespace AlitaSystemCore.Extras.StreamingConversion;

public class ConstructionForMP4ByFFmpeg : IConstructionForMP4ByFFmpeg, IDisposable
{
    internal VideoBuildParameter VideoBuildParameter;
    internal unsafe AVStream* PStream;
    internal unsafe AVCodecContext* PCodecContext;
    internal unsafe AVFormatContext* PFormatContext;
    internal readonly AVCodecID CodecId = AVCodecID.AV_CODEC_ID_H264;
    internal readonly AVPixelFormat SourcePixelFormat = AVPixelFormat.AV_PIX_FMT_BGR24;
    internal readonly AVPixelFormat DestinationPixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;
    internal VideoFrameConverter VideoFrameConverter;
    private int _frameNumber;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConstructionForMP4ByFFmpeg()
    {
        
    }

    /// <summary>
    /// 构建视频
    /// </summary>
    public Task Build(VideoBuildParameter videoBuildParameter)
    {
        unsafe
        {
            _frameNumber        = 0;
            VideoBuildParameter = videoBuildParameter;
            PFormatContext      = ffmpeg.avformat_alloc_context();

            var pFormatContext = PFormatContext;
            ffmpeg.avformat_alloc_output_context2(&pFormatContext, null, "mp4", videoBuildParameter.VideoFilePath)
                    .ThrowExceptionIfError();

            // 设置视频流参数
            var pCodec    = ffmpeg.avcodec_find_encoder(CodecId);
            var outFormat = ffmpeg.av_guess_format("mp4", null, null);
            PFormatContext->oformat = outFormat;

            // 设置编码参数
            PStream = ffmpeg.avformat_new_stream(PFormatContext, pCodec);
            PStream->time_base = new AVRational
            {
                num = 1,
                den = VideoBuildParameter.Fps
            };

            PCodecContext         = ffmpeg.avcodec_alloc_context3(pCodec);
            PCodecContext->width  = VideoBuildParameter.ImageSize.Width;
            PCodecContext->height = VideoBuildParameter.ImageSize.Height;
            PCodecContext->time_base = new AVRational
            {
                num = 1,
                den = VideoBuildParameter.Fps
            };
            PCodecContext->codec_type    =  AVMediaType.AVMEDIA_TYPE_VIDEO;
            PCodecContext->has_b_frames  =  0;
            PCodecContext->max_b_frames  =  0;
            PCodecContext->framerate.num =  VideoBuildParameter.Fps; // 帧率
            PCodecContext->framerate.den =  1;
            PCodecContext->gop_size      =  VideoBuildParameter.Fps; // 每秒1个关键帧
            PCodecContext->has_b_frames  =  0;
            PCodecContext->max_b_frames  =  0;
            PCodecContext->flags         |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
            PCodecContext->pix_fmt       =  AVPixelFormat.AV_PIX_FMT_YUV420P; // 像素格式
            PCodecContext->sample_rate   =  0;                                // 将sample_rate设置为0，表示不需要处理音频

            ffmpeg.av_opt_set(PCodecContext->priv_data, "preset", "veryslow", 0);

            // 打开音视频编码器
            ffmpeg.avcodec_open2(PCodecContext, pCodec, null)
                    .ThrowExceptionIfError();

            ffmpeg.avcodec_parameters_from_context(PStream->codecpar, PCodecContext);

            // 写入文件头
            if ((PFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
            {
                // 打开输出文件
                ffmpeg.avio_open(&PFormatContext->pb, VideoBuildParameter.VideoFilePath, ffmpeg.AVIO_FLAG_WRITE)
                        .ThrowExceptionIfError();
            }

            ffmpeg.avformat_write_header(PFormatContext, null)
                    .ThrowExceptionIfError();

            VideoFrameConverter = new VideoFrameConverter(VideoBuildParameter.ImageSize, SourcePixelFormat,
                    VideoBuildParameter.ImageSize, DestinationPixelFormat);

            foreach (var path in VideoBuildParameter.ImagePaths)
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
                        pts = _frameNumber * PCodecContext->time_base.den / PCodecContext->time_base.num,
                        time_base = new AVRational
                        {
                            num = 1,
                            den = VideoBuildParameter.Fps
                        },
                        data = new byte_ptr8
                        {
                            [0] = pBitmapData
                        },
                        linesize = new int8
                        {
                            [0] = bitmapData.Length / VideoBuildParameter.ImageSize.Height
                        },
                        height = VideoBuildParameter.ImageSize.Height
                    };
                    var convertedFrame = VideoFrameConverter.Convert(frame);
                    Encode(convertedFrame);
                }

                Console.WriteLine($"frame: {_frameNumber}");
                _frameNumber++;
            }

            Drain();
            
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 
    /// </summary>
    private unsafe void Encode(AVFrame frame)
    {
        var pPacket = ffmpeg.av_packet_alloc();

        try
        {
            ffmpeg.avcodec_send_frame(PCodecContext, &frame)
                    .ThrowExceptionIfError();

            bool hasFinishedWithThisFrame;

            do
            {
                ffmpeg.av_packet_unref(pPacket);

                var response = ffmpeg.avcodec_receive_packet(PCodecContext, pPacket);

                bool isPacketValid;

                if (response == 0)
                {
                    isPacketValid = true;
                    hasFinishedWithThisFrame = false;
                }
                else if (response == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                {
                    isPacketValid = false;
                    hasFinishedWithThisFrame = true;
                }
                else if (response == ffmpeg.AVERROR(ffmpeg.AVERROR_EOF))
                {
                    isPacketValid = false;
                    hasFinishedWithThisFrame = true;
                }
                else
                {
                    throw new InvalidOperationException($"error from avcodec_receive_packet: {response}");
                }

                if (!isPacketValid)
                    continue;

                ffmpeg.av_packet_rescale_ts(pPacket, PCodecContext->time_base, PStream->time_base);
                ffmpeg.av_interleaved_write_frame(PFormatContext, pPacket);
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
    private unsafe void Drain()
    {
        var pPacket = ffmpeg.av_packet_alloc();

        try
        {
            ffmpeg.avcodec_send_frame(PCodecContext, null)
                    .ThrowExceptionIfError();

            bool hasFinishedDraining;

            do
            {
                ffmpeg.av_packet_unref(pPacket);

                var response = ffmpeg.avcodec_receive_packet(PCodecContext, pPacket);

                bool isPacketValid;

                if (response == 0)
                {
                    isPacketValid = true;
                    hasFinishedDraining = false;
                }
                else if (response == ffmpeg.AVERROR(ffmpeg.AVERROR_EOF))
                {
                    isPacketValid = false;
                    hasFinishedDraining = true;
                }
                else
                {
                    isPacketValid = false;
                    hasFinishedDraining = true;
                }

                if (!isPacketValid)
                    continue;

                ffmpeg.av_packet_rescale_ts(pPacket, PCodecContext->time_base, PStream->time_base);
                ffmpeg.av_interleaved_write_frame(PFormatContext, pPacket);
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
    private byte[] GetBitmapData(Bitmap frameBitmap)
    {
        var bitmapData = frameBitmap.LockBits(new Rectangle(Point.Empty, frameBitmap.Size), ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

        try
        {
            var length = bitmapData.Stride * bitmapData.Height;
            var data = new byte[length];
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
    public unsafe void Dispose()
    {
        VideoFrameConverter.Dispose();

        var pFormatContext = PFormatContext;
        ffmpeg.av_write_trailer(pFormatContext)
                .ThrowExceptionIfError();

        // 1、释放avcodec_free_context
        var pCodecContext = PCodecContext;
        ffmpeg.avcodec_free_context(&pCodecContext);

        // 2、释放pFormatContext
        if ((pFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
        {
            ffmpeg.avio_closep(&pFormatContext->pb);
        }

        ffmpeg.avformat_free_context(pFormatContext);
    }
}