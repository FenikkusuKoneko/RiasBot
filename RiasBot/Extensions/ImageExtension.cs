using ImageMagick;
using ImageSharp;
using SixLabors.Shapes;
using System;

namespace RiasBot.Extensions
{
    public static class ImageExtension
    {
        public static void Round(this Image<Rgba32> img, float cornerRadius)
        {
            var corners = BuildCorners(img.Width, img.Height, cornerRadius);
            img.Fill(Rgba32.Transparent, corners, new GraphicsOptions(true)
            {
                BlenderMode = ImageSharp.PixelFormats.PixelBlenderMode.Src
            });
        }

        public static IPathCollection BuildCorners(int imageWidth, int imageHeight, float cornerRadius)
        {
            var rect = new RectangularePolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);
            var cornerToptLeft = rect.Clip(new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius));

            float rightPos = imageWidth - cornerToptLeft.Bounds.Width + 1;
            float bottomPos = imageHeight - cornerToptLeft.Bounds.Height + 1;

            var cornerTopRight = cornerToptLeft.RotateDegree(90).Translate(rightPos, 0);
            var cornerBottomLeft = cornerToptLeft.RotateDegree(-90).Translate(0, bottomPos);
            var cornerBottomRight = cornerToptLeft.RotateDegree(180).Translate(rightPos, bottomPos);

            return new PathCollection(cornerToptLeft, cornerBottomLeft, cornerTopRight, cornerBottomRight);
        }

        public static IPathCollection AvatarStroke(int x, int y, float radius)
        {
            var circleStroke = new EllipsePolygon(x, y, radius + 3);
            var circle = new EllipsePolygon(x, y, radius);

            var result = circleStroke.Clip(circle);

            return new PathCollection(result);
        }

        public static int PerceivedBrightness(Rgba32 c)
        {
            return (int)Math.Sqrt(
            c.R * c.R * .299 +
            c.G * c.G * .587 +
            c.B * c.B * .114);
        }
    }
}
