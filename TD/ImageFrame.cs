using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace TD
{
    class ImageFrame
    {
        public int Width;
        public int Height;
        public short[,] Pixels;
        public TGE.Color[] ColorTable;
        OctreeQuantizer quantizer = new OctreeQuantizer(16, 8);
        public static float[] frameDelay;

        public void SetFrameData(Bitmap bmp)
        {
            GetFrameDelay(bmp);
            Width = bmp.Width;
            Height = bmp.Height;
            ColorTable = GetColorTable(bmp);
            if (ColorTable.Length > 16)
            {
                var compressed = CompressColors(bmp);
                ColorTable = GetColorTable(compressed);
            }
            TGE.Color[,] Colors = GetColors(bmp);
            Pixels = new short[Width, Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Pixels[x, y] = GetClosestColorIndex(Colors[x, y]);
                }
            }
        }

        void GetFrameDelay(Bitmap bmp)
        {
            if (frameDelay == null)
            {
                PropertyItem frameDelayItem = bmp.GetPropertyItem(0x5100);
                var FrameCount = bmp.GetFrameCount(FrameDimension.Time);
                // If the image does not have a frame delay, we just return 0.                                     
                if (frameDelayItem != null)
                {
                    // Convert the frame delay from byte[] to int
                    byte[] values = frameDelayItem.Value;
                    frameDelay = new float[FrameCount];
                    for (int i = 0; i < FrameCount; ++i)
                    {
                        frameDelay[i] = (values[i * 4] + 256 * values[i * 4 + 1] + 256 * 256 * values[i * 4 + 2] + 256 * 256 * 256 * values[i * 4 + 3]) / 100f;
                    }
                }
            }
        }

        TGE.Color[] GetColorTable(Bitmap bmp)
        {
            BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int bytesPerPixel = 4;
            int byteCount = bitmapData.Stride * bmp.Height;
            byte[] bytes = new byte[byteCount];
            IntPtr ptrFirstPixel = bitmapData.Scan0;
            Marshal.Copy(ptrFirstPixel, bytes, 0, bytes.Length);
            int heightInPixels = bitmapData.Height;
            int widthInBytes = bitmapData.Width * bytesPerPixel;
            TGE.Color[] colors = new TGE.Color[bmp.Width * bmp.Height];
            for (int y = 0; y < heightInPixels; y++)
            {
                int currentLine = y * bitmapData.Stride;
                for (int x = 0; x < widthInBytes; x += bytesPerPixel)
                {
                    byte B = bytes[currentLine + x];
                    byte G = bytes[currentLine + x + 1];
                    byte R = bytes[currentLine + x + 2];
                    colors[x / bytesPerPixel + y * Width] = TGE.Color.FromArgb(R, G, B);
                }
            }
            bmp.UnlockBits(bitmapData);
            return colors.Distinct().ToArray();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        Bitmap CompressColors(Bitmap bmp)
        {
            var bmp1 = quantizer.Quantize(bmp);
            return bmp1;
        }
        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        TGE.Color[,] GetColors(Bitmap bmp)
        {
            BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);

            int bytesPerPixel = Bitmap.GetPixelFormatSize(bmp.PixelFormat) / 8;
            int byteCount = bitmapData.Stride * bmp.Height;
            byte[] bytes = new byte[byteCount];
            IntPtr ptrFirstPixel = bitmapData.Scan0;
            Marshal.Copy(ptrFirstPixel, bytes, 0, bytes.Length);
            int heightInPixels = bitmapData.Height;
            int widthInBytes = bitmapData.Width * bytesPerPixel;
            TGE.Color[,] colors = new TGE.Color[bmp.Width, bmp.Height];
            for (int y = 0; y < heightInPixels; y++)
            {
                int currentLine = y * bitmapData.Stride;
                for (int x = 0; x < widthInBytes; x += bytesPerPixel)
                {
                    byte B = bytes[currentLine + x];
                    byte G = bytes[currentLine + x + 1];
                    byte R = bytes[currentLine + x + 2];
                    colors[x / bytesPerPixel, y] = TGE.Color.FromArgb(R, G, B);
                }
            }
            bmp.UnlockBits(bitmapData);
            return colors;
        }

        short GetClosestColorIndex(TGE.Color c)
        {
            short closestColorIndex = 0;
            float closestDist = float.MaxValue;
            for (short i = 0; i < ColorTable.Length; i++)
            {
                float dist = GetColorSqrDist(ColorTable[i], c);
                if (dist < closestDist)
                {
                    closestColorIndex = i;
                    closestDist = dist;
                }
            }
            return closestColorIndex;
        }

        float GetColorSqrDist(TGE.Color c1, TGE.Color c2)
        {
            return (((c2.R - c1.R) * 0.30f) * ((c2.R - c1.R) * 0.30f)) + (((c2.G - c1.G) * 0.30f) * ((c2.G - c1.G) * 0.30f)) + (((c2.B - c1.B) * 0.30f) * ((c2.B - c1.B) * 0.30f));
        }
    }
}
