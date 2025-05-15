using System;
using System.Linq;
using OpenTK.Graphics.OpenGL;

namespace ShaderPluginGUI
{
    public enum PSPluginErrorCodes : short
    {
        NoError = 0,
        UserCanceledError = -128,
        CoercedParamError = 2,
        ReadError = -19,
        WriteError = -20,
        OpenError = -23,
        DiskFullError = -34,
        IOError = -36,
        eofErr = -39, // Also - end of descriptor error.
        fnfErr = -43,
        vLckdErr = -46,
        fLckdErr = -45,
        ParamError = -50,
        MemoryFullError = -108,
        NullHandleErr = -109,
        memWZErr = -111
    }

    public enum DrawModes : int
    {
        RGBA = 0,
        RGB,
        Red,
        Green,
        Blue,
        Alpha,
    }

    [Flags]
    public enum TexturePrepareMode : int
    {
        Nothing = 0,
        RXXX_TO_RRR1 = 1,
        RAXX_TO_RRRA = 2,
        RGBA_TO_RAGB = 4,
        RGXX_TO_RG01 = 8,
        RGAX_TO_RG0A = 16,
        RGBA_TO_RGAB = 32,
        RGBX_TO_RGB1 = 64,
        RGBA_TO_RGBA = 128,
        Depth16_PS_To_GL = 256,
        Depth16_GL_To_PS = 512,
        FlipUV_Y = 1024
    }

    public enum MultiPassBuffers : int
    {
        NoBuffers = 0,
        BufferA = 1,
        BufferAB = 2,
        BufferABC = 3,
        BufferABCD = 4
    }

    public enum MultiPassBuffersDrawMode : int
    {
        NoBuffers = 0,
        BufferA = 1,
        BufferB = 2,
        BufferC = 3,
        BufferD = 4
    }

    [Flags]
    public enum MouseUniformState : int
    {
        Nothing = 0,
        Pressed = 1,
        Released = 2,
        Move = 4,
    }

    public static class OpenGLComboBoxItemSource
    {
        // Return filtered TextureMinFilters for ComboBoxes
        public static TextureMinFilter[] GetTextureMinFilters(bool MipMaps)
        {
            TextureMinFilter[] TextureMinFilters = (TextureMinFilter[])Enum.GetValues(typeof(TextureMinFilter));
            return Array.FindAll(TextureMinFilters, (TextureMinFilter MinFilter) =>
            {
                string MinFilterStr = MinFilter.ToString();
                return !MinFilterStr.EndsWith("Sgis") && !MinFilterStr.EndsWith("Sgix") && (MipMaps || MinFilterStr.IndexOf("Mipmap") == -1);
            });
        }

        // Return filtered TextureMagFilters for ComboBoxes
        public static TextureMagFilter[] GetTextureMagFilters()
        {
            TextureMagFilter[] TextureMagFilters = (TextureMagFilter[])Enum.GetValues(typeof(TextureMagFilter));
            return Array.FindAll(TextureMagFilters, (TextureMagFilter MagFilter) =>
            {
                string MagFilterStr = MagFilter.ToString();
                return !MagFilterStr.EndsWith("Sgis") && !MagFilterStr.EndsWith("Sgix");
            });
        }

        // Return filtered TextureWrapModes for ComboBoxes
        public static TextureWrapMode[] GetTextureWrapModes()
        {
            TextureWrapMode[] TextureWrapModes = (TextureWrapMode[])Enum.GetValues(typeof(TextureWrapMode));
            return Array.FindAll(TextureWrapModes, (TextureWrapMode WrapMode) =>
            {
                string WrapModeStr = WrapMode.ToString();
                return !WrapModeStr.EndsWith("Sgis") && !WrapModeStr.EndsWith("Nv") && !WrapModeStr.EndsWith("Arb");
            }).Distinct().ToArray();
        }
    }
}
