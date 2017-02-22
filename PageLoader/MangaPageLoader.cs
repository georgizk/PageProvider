using System;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;

namespace PageProvider.PageLoader
{
    public class MangaPageLoader
    {
        private IAsyncOperation<BitmapEncoder> _encoderCreate;
        private IAsyncOperation<BitmapDecoder> _decoderCreate;
        private IAsyncOperation<SoftwareBitmap> _getBitmap;
        private IAsyncAction _flushAction;

        public IAsyncOperation<SoftwareBitmap> AsImageAsync(IRandomAccessStream data)
        {
            return randomAccessStreamToBitmap(data);    
        }

        private IAsyncOperation<SoftwareBitmap> randomAccessStreamToBitmap(IRandomAccessStream stream)
        {
            if (_decoderCreate != null)
            {
                _decoderCreate.Cancel();
                _decoderCreate = null;
            }

            if (_getBitmap != null)
            {
                _getBitmap.Cancel();
                _getBitmap = null;
            }

            var r = AsyncInfo.Run(async (workItem) =>
            {
                workItem.ThrowIfCancellationRequested();
                _decoderCreate = BitmapDecoder.CreateAsync(stream);
                var decoder = await _decoderCreate;
                workItem.ThrowIfCancellationRequested();
                _getBitmap = decoder.GetSoftwareBitmapAsync();
                var bmp = await _getBitmap;
                workItem.ThrowIfCancellationRequested();
                if (bmp.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                    bmp.BitmapAlphaMode == BitmapAlphaMode.Straight)
                {
                    bmp = SoftwareBitmap.Convert(bmp, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }
                return bmp;
            });
            return r;
        }

        public IAsyncOperation<SoftwareBitmap> AsThumbnailAsync(IRandomAccessStream data, double width, double height)
        {
            if (_decoderCreate != null)
            {
                _decoderCreate.Cancel();
                _decoderCreate = null;
            }

            if (_encoderCreate != null)
            {
                _encoderCreate.Cancel();
                _encoderCreate = null;
            }

            if (_flushAction != null)
            {
                _flushAction.Cancel();
                _flushAction = null;
            }

            var r = AsyncInfo.Run(async (workItem) =>
            {
                workItem.ThrowIfCancellationRequested();
                _decoderCreate = BitmapDecoder.CreateAsync(data);
                var decoder = await _decoderCreate;
                var resizedStream = new InMemoryRandomAccessStream();
                workItem.ThrowIfCancellationRequested();
                _encoderCreate = BitmapEncoder.CreateForTranscodingAsync(resizedStream, decoder);
                var encoder = await _encoderCreate;
                double widthRatio = width / decoder.PixelWidth;
                double heightRatio = height / decoder.PixelHeight;
                double scaleRatio = Math.Min(widthRatio, heightRatio);
                uint aspectHeight = (uint)Math.Floor(decoder.PixelHeight * scaleRatio);
                uint aspectWidth = (uint)Math.Floor(decoder.PixelWidth * scaleRatio);

                encoder.BitmapTransform.ScaledHeight = aspectHeight;
                encoder.BitmapTransform.ScaledWidth = aspectWidth;
                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Cubic;
                workItem.ThrowIfCancellationRequested();
                _flushAction = encoder.FlushAsync();
                await _flushAction;
                resizedStream.Seek(0);
                workItem.ThrowIfCancellationRequested();
                var src = await randomAccessStreamToBitmap(resizedStream);
                return src;
            });

            return r;
        }
    }
}
