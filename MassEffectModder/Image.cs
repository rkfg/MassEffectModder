/*
 * MassEffectModder
 *
 * Copyright (C) 2016-2017 Pawel Kolodziejski <aquadran at users.sourceforge.net>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
 *
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using StreamHelpers;
using AmaroK86.ImageFormat;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MassEffectModder
{
    public enum PixelFormat
    {
        Unknown, DXT1, DXT3, DXT5, ATI2, V8U8, ARGB, RGB, G8
    }

    public class MipMap
    {
        public byte[] data { get; private set; }
        public int width { get; private set; }
        public int height { get; private set; }
        public int origWidth { get; private set; }
        public int origHeight { get; private set; }

        public MipMap(byte[] src, int w, int h, PixelFormat format)
        {
            width = origWidth = w;
            height = origHeight = h;

            if (format == PixelFormat.DXT1 ||
                format == PixelFormat.DXT3 ||
                format == PixelFormat.DXT5)
            {
                if (width < 4)
                    width = 4;
                if (height < 4)
                    height = 4;
            }

            if (src.Length != getBufferSize(width, height, format))
                throw new Exception("data size is not valid");
            data = src;
        }

        static public int getBufferSize(int w, int h, PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.ARGB:
                    return 4 * w * h;
                case PixelFormat.RGB:
                    return 3 * w * h;
                case PixelFormat.V8U8:
                    return 2 * w * h;
                case PixelFormat.DXT3:
                case PixelFormat.DXT5:
                case PixelFormat.ATI2:
                case PixelFormat.G8:
                    return w * h;
                case PixelFormat.DXT1:
                    return (w * h) / 2;
                default:
                    throw new Exception("unknown format");
            }
        }
    }

    public partial class Image
    {
        public enum ImageFormat
        {
            Unknown, DDS, PNG, BMP, TGA, JPEG
        }

        public List<MipMap> mipMaps { get; private set; }
        public bool hasAlpha { get; private set; }
        public PixelFormat pixelFormat { get; private set; } = PixelFormat.Unknown;

        public Image(string fileName, ImageFormat format = ImageFormat.Unknown)
        {
            if (format == ImageFormat.Unknown)
                format = DetectImageByFilename(fileName);

            using (FileStream stream = File.OpenRead(fileName))
            {
                LoadImage(new MemoryStream(stream.ReadToBuffer(stream.Length)), format);
            }
        }

        public Image(MemoryStream stream, ImageFormat format)
        {
            LoadImage(stream, format);
        }

        public Image(MemoryStream stream, string extension)
        {
            LoadImage(stream, DetectImageByExtension(extension));
        }

        public Image(byte[] image, ImageFormat format)
        {
            LoadImage(new MemoryStream(image), format);
        }

        public Image(byte[] image, string extension)
        {
            LoadImage(new MemoryStream(image), DetectImageByExtension(extension));
        }

        public Image(List<MipMap> mipmaps, PixelFormat pixelFmt)
        {
            mipMaps = mipmaps;
            pixelFormat = pixelFmt;
            if (pixelFormat == PixelFormat.DXT1)
                hasAlpha = true;
        }

        private ImageFormat DetectImageByFilename(string fileName)
        {
            return DetectImageByExtension(Path.GetExtension(fileName));
        }

        private ImageFormat DetectImageByExtension(string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".dds":
                    return ImageFormat.DDS;
                case ".tga":
                    return ImageFormat.TGA;
                case ".bmp":
                    return ImageFormat.BMP;
                case ".png":
                    return ImageFormat.PNG;
                case ".jpg":
                case ".jpeg":
                    return ImageFormat.JPEG;
                default:
                    return ImageFormat.Unknown;
            }
        }

        private void LoadImage(MemoryStream stream, ImageFormat format)
        {
            mipMaps = new List<MipMap>();
            switch (format)
            {
                case ImageFormat.DDS:
                    {
                        LoadImageDDS(stream, format);
                        break;
                    }
                case ImageFormat.TGA:
                    {
                        LoadImageTGA(stream, format);
                        break;
                    }
                case ImageFormat.BMP:
                    {
                        LoadImageBMP(stream, format);
                        break;
                    }
                case ImageFormat.PNG:
                case ImageFormat.JPEG:
                    {
                        BitmapSource frame = null;
                        if (format == ImageFormat.PNG)
                            frame = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.Default).Frames[0];
                        else if (format == ImageFormat.JPEG)
                            frame = new JpegBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.Default).Frames[0];

                        if (!checkPowerOfTwo((int)frame.Width) ||
                            !checkPowerOfTwo((int)frame.Height))
                            throw new Exception("dimensions not power of two");

                        FormatConvertedBitmap srcBitmap = new FormatConvertedBitmap();
                        srcBitmap.BeginInit();
                        srcBitmap.Source = frame;
                        srcBitmap.DestinationFormat = PixelFormats.Bgra32;
                        srcBitmap.EndInit();

                        byte[] pixels = new byte[srcBitmap.PixelWidth * srcBitmap.PixelHeight * 4];
                        frame.CopyPixels(pixels, srcBitmap.PixelWidth * 4, 0);

                        MipMap mipmap = new MipMap(pixels, srcBitmap.PixelWidth, srcBitmap.PixelHeight, PixelFormat.ARGB);
                        mipMaps.Add(mipmap);
                        break;
                    }
                default:
                    throw new Exception();
            }
        }

        public static byte[] convertRawToARGB(byte[] src, int w, int h, PixelFormat format, bool stripAlpha = false)
        {
            byte[] tmpData;
            switch (format)
            {
                case PixelFormat.DXT1: tmpData = DDSImage.UncompressDXT1(src, w, h, stripAlpha); break;
                case PixelFormat.DXT3: tmpData = DDSImage.UncompressDXT3(src, w, h, stripAlpha); break;
                case PixelFormat.DXT5: tmpData = DDSImage.UncompressDXT5(src, w, h, stripAlpha); break;
                case PixelFormat.ATI2: tmpData = DDSImage.UncompressATI2(src, w, h); break;
                case PixelFormat.ARGB: tmpData = src; break;
                case PixelFormat.RGB: tmpData = RGBToARGB(src, w, h); break;
                case PixelFormat.V8U8: tmpData = V8U8ToARGB(src, w, h); break;
                case PixelFormat.G8: tmpData = G8ToARGB(src, w, h); break;
                default:
                    throw new Exception("invalid texture format " + format);
            }
            return tmpData;
        }

        public static byte[] convertRawToRGB(byte[] src, int w, int h, PixelFormat format, bool stripAlpha = false)
        {
            byte[] tmpData = convertRawToARGB(src, w, h, format, stripAlpha);
            byte[] tmpDataNew = new byte[w * h * 3];
            for (int i = 0; i < w * h; i++)
            {
                tmpDataNew[3 * i + 0] = tmpData[4 * i + 0];
                tmpDataNew[3 * i + 1] = tmpData[4 * i + 1];
                tmpDataNew[3 * i + 2] = tmpData[4 * i + 2];
            }
            return tmpDataNew;
        }

        public static Bitmap convertRawToBitmapARGB(byte[] src, int w, int h, PixelFormat format)
        {
            byte[] tmpData = convertRawToARGB(src, w, h, format, true);
            Bitmap bitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            Marshal.Copy(tmpData, 0, bitmapData.Scan0, tmpData.Length);
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        public Bitmap getBitmapARGB()
        {
            return convertRawToBitmapARGB(mipMaps[0].data, mipMaps[0].width, mipMaps[0].height, pixelFormat);
        }

        private static byte[] RGBToARGB(byte[] src, int w, int h)
        {
            byte[] tmpData = new byte[w * h * 4];
            for (int i = 0; i < w * h; i++)
            {
                tmpData[4 * i + 0] = src[3 * i + 0];
                tmpData[4 * i + 1] = src[3 * i + 1];
                tmpData[4 * i + 2] = src[3 * i + 2];
                tmpData[4 * i + 3] = 255;
            }
            return tmpData;
        }

        private static byte[] V8U8ToARGB(byte[] src, int w, int h)
        {
            byte[] tmpData = new byte[w * h * 4];
            for (int i = 0; i < w * h; i++)
            {
                tmpData[4 * i + 0] = 255;;
                tmpData[4 * i + 1] = (byte)(((sbyte)src[2 * i + 1]) + 128);
                tmpData[4 * i + 2] = (byte)(((sbyte)src[2 * i + 0]) + 128);
                tmpData[4 * i + 3] = 255;
            }
            return tmpData;
        }

        private static byte[] G8ToARGB(byte[] src, int w, int h)
        {
            byte[] tmpData = new byte[w * h * 4];
            for (int i = 0; i < w * h; i++)
            {
                tmpData[4 * i + 0] = src[i];
                tmpData[4 * i + 1] = src[i];
                tmpData[4 * i + 2] = src[i];
                tmpData[4 * i + 3] = 255;
            }

            return tmpData;
        }

        public static PngBitmapEncoder convertToPng(byte[] src, int w, int h, PixelFormat format, bool stripAlpha = false)
        {
            byte[] tmpData = convertRawToARGB(src, w, h, format, stripAlpha);
            PngBitmapEncoder png = new PngBitmapEncoder();
            BitmapSource image = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, tmpData, w * 4);
            png.Frames.Add(BitmapFrame.Create(image));
            return png;
        }

        public static PixelFormat convertFormat(string format)
        {
            switch (format)
            {
                case "PF_DXT1":
                    return PixelFormat.DXT1;
                case "PF_DXT3":
                    return PixelFormat.DXT3;
                case "PF_DXT5":
                    return PixelFormat.DXT5;
                case "PF_NormalMap_HQ":
                    return PixelFormat.ATI2;
                case "PF_V8U8":
                    return PixelFormat.V8U8;
                case "PF_A8R8G8B8":
                    return PixelFormat.ARGB;
                case "PF_R8G8B8":
                    return PixelFormat.RGB;
                case "PF_G8":
                    return PixelFormat.G8;
                default:
                    throw new Exception("invalid texture format");
            }
        }

        public bool checkPowerOfTwo(int n)
        {
            if ((n & (n - 1)) == 0)
                return true;
            else
                return false;
        }
    }
}
