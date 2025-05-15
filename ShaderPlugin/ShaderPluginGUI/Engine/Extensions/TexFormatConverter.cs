using OpenTK.Graphics.OpenGL;
using System.Windows.Media;

using PixelFormatGL = OpenTK.Graphics.OpenGL.PixelFormat;
using PixelFormatWPF = System.Windows.Media.PixelFormat;

namespace ShaderPluginGUI
{
    public static class TexFormatConverter
    {
        public static PixelType GetGLPixelTypeFromPhotoshop(int PhotoshopDepth)
        {
            switch (PhotoshopDepth)
            {
                case 8:
                default:
                    return PixelType.UnsignedByte;

                case 16:
                    return PixelType.UnsignedShort;

                case 32:
                    return PixelType.Float;
            }
        }

        public static PixelFormatGL GetGLPixelFormat(int colorChannels, bool haveTransparency)
        {

            switch (colorChannels)
            {
                case 1:
                    return haveTransparency ? PixelFormatGL.Rg : PixelFormatGL.Red;
                case 2:
                    return haveTransparency ? PixelFormatGL.Rgb : PixelFormatGL.Rg;
                case 3:
                    return haveTransparency ? PixelFormatGL.Rgba : PixelFormatGL.Rgb;
                default:
                    return PixelFormatGL.Rgba;
            }
        }

        public static PixelFormatGL GetGLPixelFormat(PixelFormatWPF PixelFormat)
        {
            if (PixelFormat == PixelFormats.Bgr24 ||
                PixelFormat == PixelFormats.Bgr32 ||
                PixelFormat == PixelFormats.Bgr555 ||
                PixelFormat == PixelFormats.Bgr565 ||
                PixelFormat == PixelFormats.Bgr101010)
            {
                return PixelFormatGL.Bgr;
            }

            if (PixelFormat == PixelFormats.Bgra32 ||
                PixelFormat == PixelFormats.Pbgra32)
            {
                return PixelFormatGL.Bgra;
            }

            if (PixelFormat == PixelFormats.Rgb24 ||
                PixelFormat == PixelFormats.Rgb48 ||
                PixelFormat == PixelFormats.Rgb128Float)
            {
                return PixelFormatGL.Rgb;
            }

            if (PixelFormat == PixelFormats.Rgba64 ||
                PixelFormat == PixelFormats.Prgba64 ||
                PixelFormat == PixelFormats.Rgba128Float ||
                PixelFormat == PixelFormats.Prgba128Float)
            {
                return PixelFormatGL.Rgba;
            }

            if (PixelFormat == PixelFormats.Gray2 ||
                PixelFormat == PixelFormats.Gray4 ||
                PixelFormat == PixelFormats.Gray8 ||
                PixelFormat == PixelFormats.Gray16 ||
                PixelFormat == PixelFormats.Gray32Float)
            {
                return PixelFormatGL.Red;
            }

            if (PixelFormat == PixelFormats.Indexed1 ||
                PixelFormat == PixelFormats.Indexed2 ||
                PixelFormat == PixelFormats.Indexed4 ||
                PixelFormat == PixelFormats.Indexed8)
            {
                return PixelFormatGL.ColorIndex;
            }

            if (PixelFormat == PixelFormats.BlackWhite)
            {
                return PixelFormatGL.Alpha;
            }

            if (PixelFormat == PixelFormats.Cmyk32)
            {
                return PixelFormatGL.CmykExt;
            }

            return PixelFormatGL.Bgra;
        }

        public static PixelInternalFormat GetGLPixelInternalFormat(int Depth, int colorChannels, bool Transparency)
        {
            switch (Depth)
            {
                case 8:
                default:
                    switch (colorChannels)
                    {
                        case 1:
                            return Transparency ? PixelInternalFormat.Rg8 : PixelInternalFormat.R8;
                        case 2:
                            return Transparency ? PixelInternalFormat.Rgb8 : PixelInternalFormat.Rg8;
                        case 3:
                            return Transparency ? PixelInternalFormat.Rgba8 : PixelInternalFormat.Rgb8;
                        default:
                            return PixelInternalFormat.Rgba8;
                    }
                case 16:
                    switch (colorChannels)
                    {
                        case 1:
                            return Transparency ? PixelInternalFormat.Rg16f : PixelInternalFormat.R16f;
                        case 2:
                            return Transparency ? PixelInternalFormat.Rgb16f : PixelInternalFormat.Rg16f;
                        case 3:
                            return Transparency ? PixelInternalFormat.Rgba16f : PixelInternalFormat.Rgb16f;
                        default:
                            return PixelInternalFormat.Rgba16f;
                    }
                case 32:
                    switch (colorChannels)
                    {
                        case 1:
                            return Transparency ? PixelInternalFormat.Rg32f : PixelInternalFormat.R32f;
                        case 2:
                            return Transparency ? PixelInternalFormat.Rgb32f : PixelInternalFormat.Rg32f;
                        case 3:
                            return Transparency ? PixelInternalFormat.Rgba32f : PixelInternalFormat.Rgb32f;
                        default:
                            return PixelInternalFormat.Rgba16f;
                    }
            }
        }

        public static PixelInternalFormat GetGLPixelInternalFormat(PixelFormatWPF PixelFormat)
        {
            if (PixelFormat == PixelFormats.Bgr24 ||
                PixelFormat == PixelFormats.Rgb24)
            {
                return PixelInternalFormat.Rgb8;
            }

            if (PixelFormat == PixelFormats.Bgr32 ||
                PixelFormat == PixelFormats.Bgra32 ||
                PixelFormat == PixelFormats.Pbgra32)
            {
                return PixelInternalFormat.Rgba8;
            }

            if (PixelFormat == PixelFormats.Rgba64 ||
                PixelFormat == PixelFormats.Prgba64)
            {
                return PixelInternalFormat.Rgba16;
            }

            if (PixelFormat == PixelFormats.Gray8 ||
                PixelFormat == PixelFormats.Indexed8)
            {
                return PixelInternalFormat.Alpha8;
            }

            if (PixelFormat == PixelFormats.Gray16)
            {
                return PixelInternalFormat.Alpha16;
            }

            if (PixelFormat == PixelFormats.Gray32Float)
            {
                return PixelInternalFormat.R32f;
            }

            if (PixelFormat == PixelFormats.Rgb48)
            {
                return PixelInternalFormat.Rgb12;
            }

            if (PixelFormat == PixelFormats.Rgb128Float)
            {
                return PixelInternalFormat.Rgb32f;
            }

            if (PixelFormat == PixelFormats.Rgba128Float ||
                PixelFormat == PixelFormats.Prgba128Float)
            {
                return PixelInternalFormat.Rgba32f;
            }

            if (PixelFormat == PixelFormats.Bgr555)
            {
                return PixelInternalFormat.Rgb5;
            }

            if (PixelFormat == PixelFormats.Bgr565)
            {
                return PixelInternalFormat.R5G6B5IccSgix;
            }

            if (PixelFormat == PixelFormats.Bgr101010)
            {
                return PixelInternalFormat.Rgb10;
            }

            if (PixelFormat == PixelFormats.Indexed1 ||
                PixelFormat == PixelFormats.BlackWhite)
            {
                return PixelInternalFormat.One;
            }

            if (PixelFormat == PixelFormats.Gray2 ||
                PixelFormat == PixelFormats.Indexed2)
            {
                return PixelInternalFormat.Two;
            }

            if (PixelFormat == PixelFormats.Gray4 ||
                PixelFormat == PixelFormats.Indexed4)
            {
                return PixelInternalFormat.Four;
            }

            if (PixelFormat == PixelFormats.Cmyk32)
            {
                return PixelInternalFormat.Rgba8;
            }

            return PixelInternalFormat.Rgba;
        }

        public static PixelFormatWPF GetWPFPixelFormat(PixelInternalFormat InternalFormat)
        {
            switch (InternalFormat)
            {
                case PixelInternalFormat.Alpha:
                case PixelInternalFormat.Alpha8:
                case PixelInternalFormat.R8:
                case PixelInternalFormat.R8ui:
                    return PixelFormats.Gray8;

                case PixelInternalFormat.Rgb:
                case PixelInternalFormat.Rgb8:
                case PixelInternalFormat.Rgb8ui:
                    return PixelFormats.Bgr24;

                case PixelInternalFormat.Rgba:
                case PixelInternalFormat.Rgba8:
                case PixelInternalFormat.Rgba8ui:
                    return PixelFormats.Bgra32;

                case PixelInternalFormat.Alpha16:
                case PixelInternalFormat.R16:
                case PixelInternalFormat.R16ui:
                    return PixelFormats.Gray16;

                case PixelInternalFormat.Rgb5:
                case PixelInternalFormat.Rgb5A1:
                    return PixelFormats.Bgr555;

                case PixelInternalFormat.Rgb16:
                    return PixelFormats.Rgb48;

                case PixelInternalFormat.Rgba16:
                    return PixelFormats.Rgba64;

                case PixelInternalFormat.R5G6B5IccSgix:
                    return PixelFormats.Bgr565;

                default:
                    return PixelFormats.Bgra32;
            }
        }
    }
}
