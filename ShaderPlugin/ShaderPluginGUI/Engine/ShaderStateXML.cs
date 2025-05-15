using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using OpenTK.Graphics.OpenGL;

namespace ShaderPluginGUI
{
    [Serializable]
    [XmlRoot(ElementName = "Shader")]
    public class ShaderStateXML
    {
        public const string LastShaderFile = "LastShader.xml";

        #region Shaders Data
        public bool AutoCompileAfterLoading = true;
        public bool AutoPlayAfterLoading = true;
        public bool UseMipMaps = false;

        public MultiPassBuffers MultiPassBuffers = MultiPassBuffers.NoBuffers;
        
        public string CommonCode = String.Empty;
        public ShaderStateBuffer Image = null;
        public ShaderStateBuffer BufferA = null;
        public ShaderStateBuffer BufferB = null;
        public ShaderStateBuffer BufferC = null;
        public ShaderStateBuffer BufferD = null;
        #endregion

        public static bool Save(ShaderStateXML State, string FullPath)
        {
            if (File.Exists(FullPath))
            {
                try
                {
                    if (Load(FullPath) == State)
                    {
                        return true;
                    }
                }
                catch { }
            }

            try
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(FullPath, new XmlWriterSettings() { Indent = true, IndentChars = "\t", OmitXmlDeclaration = true }))
                {
                    #region Remove "xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" ..."
                    XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                    namespaces.Add(string.Empty, string.Empty);
                    #endregion

                    XmlSerializer serializer = new XmlSerializer(typeof(ShaderStateXML));
                    serializer.Serialize(xmlWriter, State, namespaces);
                    xmlWriter.Close();
                    return File.Exists(FullPath);
                }
            }
            catch
            {
                return false;
            }
        }

        public static ShaderStateXML Load(string ShaderXMLFile)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ShaderStateXML));
            if (!File.Exists(ShaderXMLFile))
            {
                return null;
            }

            try
            {
                using (FileStream ShaderFile = new FileStream(ShaderXMLFile, FileMode.Open))
                {
                    ShaderStateXML State = (ShaderStateXML)serializer.Deserialize(ShaderFile);
                    ShaderFile.Close();
                    return State;
                }
            }
            catch
            {
                return null;
            }
        }

        public ShaderStateBuffer this[int ShaderID]
        {
            get
            {
                switch (ShaderID)
                {
                    case ShaderIDs.Image: return Image;
                    case ShaderIDs.BufferA: return BufferA;
                    case ShaderIDs.BufferB: return BufferB;
                    case ShaderIDs.BufferC: return BufferC;
                    case ShaderIDs.BufferD: return BufferD;
                    default: return null;
                }
            }
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + UseMipMaps.GetHashCode();
            hash = hash * 31 + AutoCompileAfterLoading.GetHashCode();
            hash = hash * 31 + AutoPlayAfterLoading.GetHashCode();
            hash = hash * 31 + MultiPassBuffers.GetHashCode();
            hash = hash * 31 + CommonCode.GetHashCode();
            hash = hash * 31 + Image.GetHashCode();
            if (BufferA != null) hash = hash * 31 + BufferA.GetHashCode();
            if (BufferB != null) hash = hash * 31 + BufferB.GetHashCode();
            if (BufferC != null) hash = hash * 31 + BufferC.GetHashCode();
            if (BufferD != null) hash = hash * 31 + BufferD.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            ShaderStateXML shaderXML = (ShaderStateXML)obj;
            return UseMipMaps == shaderXML.UseMipMaps && 
                AutoPlayAfterLoading == shaderXML.AutoPlayAfterLoading &&
                AutoCompileAfterLoading == shaderXML.AutoCompileAfterLoading &&
                MultiPassBuffers == shaderXML.MultiPassBuffers &&
                CommonCode == shaderXML.CommonCode &&
                Image == shaderXML.Image &&
                BufferA == shaderXML.BufferA &&
                BufferB == shaderXML.BufferB &&
                BufferC == shaderXML.BufferC &&
                BufferD == shaderXML.BufferD;
        }

        public static bool operator ==(ShaderStateXML A, ShaderStateXML B)
        {
            if (A?.GetType() == null)
            {
                return (A?.GetType() == B?.GetType());
            }

            return A.Equals(B);
        }

        public static bool operator !=(ShaderStateXML A, ShaderStateXML B)
        {
            if (A?.GetType() == null)
            {
                return (A?.GetType() != B?.GetType());
            }

            return !A.Equals(B);
        }
    }

    public struct ShaderStateTextureParams
    {
        public TextureMagFilter TextureMagFilter;
        public TextureMinFilter TextureMinFilter;
        public TextureWrapMode TextureWrapModeS;
        public TextureWrapMode TextureWrapModeT;

        public static ShaderStateTextureParams Empty { get; }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + (int)TextureMagFilter;
            hash = hash * 31 + (int)TextureMinFilter;
            hash = hash * 31 + (int)TextureWrapModeS;
            hash = hash * 31 + (int)TextureWrapModeT;
            return hash;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            ShaderStateTextureParams TextureFilters = (ShaderStateTextureParams)obj;
            return TextureMagFilter == TextureFilters.TextureMagFilter &&
                TextureMinFilter == TextureFilters.TextureMinFilter &&
                TextureWrapModeS == TextureFilters.TextureWrapModeS &&
                TextureWrapModeT == TextureFilters.TextureWrapModeT;
        }

        public static bool operator ==(ShaderStateTextureParams A, ShaderStateTextureParams B)
        {
            return A.Equals(B);
        }

        public static bool operator !=(ShaderStateTextureParams A, ShaderStateTextureParams B)
        {
            return !A.Equals(B);
        }
    }

    public class ShaderStateBuffer
    {
        public string Shader_VS;
        public string Shader_FS;
        public ShaderStateTextureParams TextureFilter;
        public ShaderStateTextureParams TextureFilterBufferA;
        public ShaderStateTextureParams TextureFilterBufferB;
        public ShaderStateTextureParams TextureFilterBufferC;
        public ShaderStateTextureParams TextureFilterBufferD;

        public ShaderStateTextureParams GetTextureFilter(int TextureUnitID)
        {
            switch (TextureUnitID)
            {
                case TextureUnitIDs.OriginalImage: return TextureFilter;
                case TextureUnitIDs.BufferA: return TextureFilterBufferA;
                case TextureUnitIDs.BufferB: return TextureFilterBufferB;
                case TextureUnitIDs.BufferC: return TextureFilterBufferC;
                case TextureUnitIDs.BufferD: return TextureFilterBufferD;
                default: return ShaderStateTextureParams.Empty;
            }
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + (Shader_VS + Shader_FS).GetHashCode();
            hash = hash * 31 + TextureFilter.GetHashCode();
            hash = hash * 31 + TextureFilterBufferA.GetHashCode();
            hash = hash * 31 + TextureFilterBufferB.GetHashCode();
            hash = hash * 31 + TextureFilterBufferC.GetHashCode();
            hash = hash * 31 + TextureFilterBufferD.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            ShaderStateBuffer BufferD = (ShaderStateBuffer)obj;
            return Shader_VS == BufferD.Shader_VS && Shader_FS == BufferD.Shader_FS && TextureFilter == BufferD.TextureFilter &&
                TextureFilterBufferA == BufferD.TextureFilterBufferA &&
                TextureFilterBufferB == BufferD.TextureFilterBufferB &&
                TextureFilterBufferC == BufferD.TextureFilterBufferC &&
                TextureFilterBufferD == BufferD.TextureFilterBufferD;
        }

        public static bool operator ==(ShaderStateBuffer A, ShaderStateBuffer B)
        {
            if (A?.GetType() == null)
            {
                return A?.GetType() == B?.GetType();
            }

            return A.Equals(B);
        }

        public static bool operator !=(ShaderStateBuffer A, ShaderStateBuffer B)
        {
            if (A?.GetType() == null)
            {
                return A?.GetType() != B?.GetType();
            }

            return !A.Equals(B);
        }
    }
}