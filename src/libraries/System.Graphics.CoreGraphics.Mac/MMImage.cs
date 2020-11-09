using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using CoreGraphics;
using Foundation;

namespace System.Graphics.CoreGraphics
{
    public class MMImage : IImage
    {
        private NSImage _image;

        public MMImage(NSImage image)
        {
            _image = image;
        }

        public float Width => (float) _image.Size.Width;

        public float Height => (float) _image.Size.Height;

        public NSImage NativeImage => _image;

        public void Save(Stream stream, ImageFormat format = ImageFormat.Png, float quality = 1)
        {
            var data = CreateRepresentation(format, quality);
            data.AsStream().CopyTo(stream);
        }

        public async Task SaveAsync(Stream stream, ImageFormat format = ImageFormat.Png, float quality = 1)
        {
            var data = CreateRepresentation(format, quality);
            await data.AsStream().CopyToAsync(stream);
        }

        private NSData CreateRepresentation(ImageFormat format = ImageFormat.Png, float quality = 1)
        {
            var previous = NSApplication.CheckForIllegalCrossThreadCalls;
            NSApplication.CheckForIllegalCrossThreadCalls = false;

            NSBitmapImageFileType type;
            NSDictionary dictionary;
            switch (format)
            {
                case ImageFormat.Jpeg:
                    type = NSBitmapImageFileType.Jpeg;
                    dictionary = new NSDictionary(new NSNumber(quality), AppKitConstants.NSImageCompressionFactor);
                    break;
                case ImageFormat.Tiff:
                    type = NSBitmapImageFileType.Tiff;
                    dictionary = new NSDictionary();
                    break;
                case ImageFormat.Gif:
                    type = NSBitmapImageFileType.Gif;
                    dictionary = new NSDictionary();
                    break;
                case ImageFormat.Bmp:
                    type = NSBitmapImageFileType.Bmp;
                    dictionary = new NSDictionary();
                    break;
                default:
                    type = NSBitmapImageFileType.Png;
                    dictionary = new NSDictionary();
                    break;
            }

            var rect = new CGRect();
            var cgimage = _image.AsCGImage(ref rect, null, null);
            var imageRep = new NSBitmapImageRep(cgimage);
            var data = imageRep.RepresentationUsingTypeProperties(type, dictionary);

            NSApplication.CheckForIllegalCrossThreadCalls = previous;

            return data;
        }

        public void Dispose()
        {
            var disp = Interlocked.Exchange(ref _image, null);
            disp?.Dispose();
        }

        public IImage Downsize(float maxWidthOrHeight, bool disposeOriginal = false)
        {
            var scaledImage = _image.ScaleImage(maxWidthOrHeight, maxWidthOrHeight, disposeOriginal);
            return new MMImage(scaledImage);
        }

        public IImage Downsize(float maxWidth, float maxHeight, bool disposeOriginal = false)
        {
            var scaledImage = _image.ScaleImage(maxWidth, maxHeight, disposeOriginal);
            return new MMImage(scaledImage);
        }

        public IImage Resize(float width, float height, ResizeMode resizeMode = ResizeMode.Fit, bool disposeOriginal = false)
        {
            using (var context = new MMBitmapExportContext((int) width, (int) height))
            {
                var fx = width / Width;
                var fy = height / Height;

                var w = Width;
                var h = Height;

                var x = 0f;
                var y = 0f;

                if (resizeMode == ResizeMode.Fit)
                {
                    if (fx < fy)
                    {
                        w *= fx;
                        h *= fx;
                    }
                    else
                    {
                        w *= fy;
                        h *= fy;
                    }

                    x = (width - w) / 2;
                    y = (height - h) / 2;
                }
                else if (resizeMode == ResizeMode.Bleed)
                {
                    if (fx > fy)
                    {
                        w *= fx;
                        h *= fx;
                    }
                    else
                    {
                        w *= fy;
                        h *= fy;
                    }

                    x = (width - w) / 2;
                    y = (height - h) / 2;
                }
                else
                {
                    w = width;
                    h = height;
                }

                context.Canvas.DrawImage(this, x, y, w, h);
                return context.Image;
            }
        }

        public void Draw(ICanvas canvas, EWRectangle dirtyRect)
        {
            canvas.DrawImage(this, dirtyRect.MinX, dirtyRect.MinY, (float)Math.Round(dirtyRect.Width), (float)Math.Round(dirtyRect.Height));
        }
    }

    public static class MMImageExtensions
    {
        public static NSImage AsNSImage(this IImage image)
        {
            if (image is MMImage macImage)
                return macImage.NativeImage;

            if (image != null)
                Logger.Warn("MMImageExtensions.AsNSImage: Unable to get NSImage from EWImage. Expected an image of type MMImage however an image of type {0} was received.", image.GetType());

            return null;
        }
    }
}