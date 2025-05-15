using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using OpenTK.Graphics.OpenGL;

namespace ShaderPluginGUI
{
    [XmlRoot(ElementName = "Config")]
    public class SettingsXML
    {
        static string SettingsPath = Path.Combine(Program.StartupPath, "Settings.xml");

        public bool Save()
        {
            try
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(SettingsPath, new XmlWriterSettings() { Indent = true, IndentChars = "\t", OmitXmlDeclaration = true }))
                {
                    #region Remove 'xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" ...'
                    XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                    namespaces.Add(string.Empty, string.Empty);
                    #endregion

                    XmlSerializer serializer = new XmlSerializer(typeof(SettingsXML));
                    serializer.Serialize(xmlWriter, this, namespaces);
                    xmlWriter.Close();
                    return File.Exists(SettingsPath);
                }
            }
            catch
            {
                return false;
            }
        }

        public static SettingsXML LoadOrDefault()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(SettingsXML));
            if (File.Exists(SettingsPath)) //If file exist
            {
                try
                {
                    using (FileStream SettingsFile = new FileStream(SettingsPath, FileMode.Open))
                    {
                        SettingsXML settings = (SettingsXML)serializer.Deserialize(SettingsFile);
                        SettingsFile.Close();
                        return settings;
                    }
                }
                catch
                {
                    return null;
                }
            }
            else //File not exist
            {
                return new SettingsXML();
            }
        }

        #region Settings
        public FormWindowState WindowState = FormWindowState.Normal;

        public TextureMagFilter PreviewMagFilter = TextureMagFilter.Nearest;
        public TextureMinFilter PreviewMinFilter = TextureMinFilter.LinearMipmapLinear;
        public bool ForceCompileWhenApply = false;

        public ColorRGBA BackgroundColor = new ColorRGBA(0.25f, 0.25f, 0.25f, 0f);

        public _PreviewSplitter PreviewSplitter = new _PreviewSplitter();
        public class _PreviewSplitter
        {
            public ColorRGBA LineColor = new ColorRGBA(0.7f, 0.7f, 0.7f, 0f);
            public float LineWidth = 2f;
        }

        public _Grid Grid = new _Grid();
        public class _Grid
        {
            public ColorRGBA GridColor1 = new ColorRGBA(0.8f, 0.8f, 0.8f, 0f);
            public ColorRGBA GridColor2 = new ColorRGBA(1.0f, 1.0f, 1.0f, 0f);
            public float GridSize = 16f;
        }
        #endregion
    }
}