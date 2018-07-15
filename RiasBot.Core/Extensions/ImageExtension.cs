using ImageMagick;
using System;
using Remotion.Linq.Clauses;

namespace RiasBot.Extensions
{
    public static class ImageExtension
    {
        public static void Roundify(this MagickImage image)
        {
            image.Alpha(AlphaOption.Set);
            using (var copy = image.Clone())
            {
                copy.Distort(DistortMethod.DePolar, 0);
                copy.VirtualPixelMethod = VirtualPixelMethod.HorizontalTile;
                copy.BackgroundColor = MagickColors.None;
                copy.Distort(DistortMethod.Polar, 0);

                image.Composite(copy, CompositeOperator.DstIn);
                image.Trim();
                image.RePage();
            }
        }

        public static int PerceivedBrightness(MagickColor c)
        {
            return (int)Math.Sqrt(
            (byte)c.R * (byte)c.R * .299 +
            (byte)c.G * (byte)c.G * .587 +
            (byte)c.B * (byte)c.B * .114);
        }
    }
}
