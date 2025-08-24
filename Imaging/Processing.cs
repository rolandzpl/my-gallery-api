using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using static Lithium.Imaging.ImageCalculator;

namespace Lithium.Imaging;

public class Processing
{
    public static async Task ConvertImage(Stream inStream, Stream outStream, ImageConvertionOptions options)
    { 
        await ConvertImage(
            inStream,
            outStream,
            options.padding,
            options.targetWidth,
            options.targetHeight,
            options.imgQual,
            options.borderWidth
        );
    }

    public static async Task ConvertImage(Stream inStream, Stream outStream, int padding, int targetWidth, int targetHeight, int? imgQual, int borderWidth)
    {
        using var image = Image.Load(inStream);
        using var canvas = new Image<Rgba32>(targetWidth, targetHeight);
        var originalImageRectangle = CalculateInternalImageRectangle(
            padding,
            targetWidth,
            targetHeight,
            image.Width,
            image.Height,
            image.Bounds().IsVertical()
                ? x => (x * image.Width) / image.Height
                : x => (x * image.Height) / image.Width);
        image.Mutate(ctx => ctx
            .Resize(new Size(originalImageRectangle.width, originalImageRectangle.height)));
        canvas.Mutate(ctx => ctx
            .Fill(Color.WhiteSmoke)
            .Fill(
                Color.White,
                new RectangleF(
                    originalImageRectangle.left - borderWidth,
                    originalImageRectangle.top - borderWidth,
                    originalImageRectangle.width + borderWidth * 2,
                    originalImageRectangle.height + borderWidth * 2))
            .DrawImage(image,
                new Point(originalImageRectangle.left, originalImageRectangle.top),
                1f));
        await canvas.SaveAsJpegAsync(outStream, new JpegEncoder() { Quality = imgQual });
    }

    public record ImageConvertionOptions(int padding, int targetWidth, int targetHeight, float lightness, int? imgQual, int borderWidth);
}

static class Extensions
{
    public static bool IsVertical(this Rectangle rectangle) => rectangle.Width < rectangle.Height;
}