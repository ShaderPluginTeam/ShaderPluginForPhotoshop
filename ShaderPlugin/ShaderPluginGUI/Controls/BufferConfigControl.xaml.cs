using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using OpenTK.Graphics.OpenGL;

namespace ShaderPluginGUI.Controls
{
    public partial class BufferConfigControl : UserControl
    {
        bool isProgrammaticControlEvent = false;

        public BufferConfigControl()
        {
            InitializeComponent();

            // Fix Enums, set it to ComboBoxes as ItemsSource
            comboBox_MagFilter.ItemsSource = OpenGLComboBoxItemSource.GetTextureMagFilters();
            comboBox_MinFilter.ItemsSource = OpenGLComboBoxItemSource.GetTextureMinFilters(UseMipMaps);
            comboBox_WrapS.ItemsSource = OpenGLComboBoxItemSource.GetTextureWrapModes();
            comboBox_WrapT.ItemsSource = OpenGLComboBoxItemSource.GetTextureWrapModes();

            SetDefaultParameters();
        }

        #region Properties
        #region Dependency Properties
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(object),
            typeof(BufferConfigControl), new PropertyMetadata("Header Title"));

        public static readonly DependencyProperty TextureMagFilterProperty = DependencyProperty.Register(nameof(TextureMagFilter), typeof(TextureMagFilter),
            typeof(BufferConfigControl), new PropertyMetadata(TextureMagFilter.Linear));

        public static readonly DependencyProperty TextureMinFilterProperty = DependencyProperty.Register(nameof(TextureMinFilter), typeof(TextureMinFilter),
            typeof(BufferConfigControl), new PropertyMetadata(TextureMinFilter.Linear));

        public static readonly DependencyProperty TextureWrapSProperty = DependencyProperty.Register(nameof(TextureWrapS), typeof(TextureWrapMode),
            typeof(BufferConfigControl), new PropertyMetadata(TextureWrapMode.Repeat));

        public static readonly DependencyProperty TextureWrapTProperty = DependencyProperty.Register(nameof(TextureWrapT), typeof(TextureWrapMode),
            typeof(BufferConfigControl), new PropertyMetadata(TextureWrapMode.Repeat));

        public static readonly DependencyProperty UseMipMapsProperty = DependencyProperty.Register(nameof(UseMipMaps), typeof(bool),
            typeof(BufferConfigControl), new PropertyMetadata(false, OnCanHasMipMapsChanged));
        #endregion

        [Category("Common")]
        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        [Category("Texture")]
        public TextureMagFilter TextureMagFilter
        {
            get => (TextureMagFilter)GetValue(TextureMagFilterProperty);
            set => SetValue(TextureMagFilterProperty, value);
        }

        [Category("Texture")]
        public TextureMinFilter TextureMinFilter
        {
            get => (TextureMinFilter)GetValue(TextureMinFilterProperty);
            set => SetValue(TextureMinFilterProperty, GetAvailableTextureMinFilter(value));
        }

        [Category("Texture")]
        public TextureWrapMode TextureWrapS
        {
            get => (TextureWrapMode)GetValue(TextureWrapSProperty);
            set => SetValue(TextureWrapSProperty, value);
        }

        [Category("Texture")]
        public TextureWrapMode TextureWrapT
        {
            get => (TextureWrapMode)GetValue(TextureWrapTProperty);
            set => SetValue(TextureWrapTProperty, value);
        }

        public ShaderStateTextureParams TextureParams
        {
            get
            {
                ShaderStateTextureParams Params = new ShaderStateTextureParams();
                Params.TextureMagFilter = TextureMagFilter;
                Params.TextureMinFilter = TextureMinFilter;
                Params.TextureWrapModeS = TextureWrapS;
                Params.TextureWrapModeT = TextureWrapT;
                return Params;
            }
            set
            {
                isProgrammaticControlEvent = true;

                TextureMagFilter = value.TextureMagFilter;
                TextureMinFilter = value.TextureMinFilter;
                TextureWrapS = value.TextureWrapModeS;
                TextureWrapT = value.TextureWrapModeT;

                isProgrammaticControlEvent = false;
            }
        }

        [Category("Texture")]
        public bool UseMipMaps
        {
            get => (bool)GetValue(UseMipMapsProperty);
            set
            {
                SetValue(UseMipMapsProperty, value);

                comboBox_MinFilter.ItemsSource = OpenGLComboBoxItemSource.GetTextureMinFilters(UseMipMaps);
                TextureMinFilter = GetAvailableTextureMinFilter(TextureMinFilter); // Remove Mipmaps from TextureMinFilter if not available
            }
        }

        private static void OnCanHasMipMapsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BufferConfigControl control = (BufferConfigControl)d;
            control.comboBox_MinFilter.ItemsSource = OpenGLComboBoxItemSource.GetTextureMinFilters((bool)e.NewValue);
        }

        TextureMinFilter GetAvailableTextureMinFilter(TextureMinFilter MinFilter)
        {
            if (!UseMipMaps)
            {
                switch (MinFilter)
                {
                    case TextureMinFilter.NearestMipmapNearest:
                    case TextureMinFilter.NearestMipmapLinear:
                        return TextureMinFilter.Nearest;

                    case TextureMinFilter.LinearMipmapNearest:
                    case TextureMinFilter.LinearMipmapLinear:
                        return TextureMinFilter.Linear;
                }
            }

            return MinFilter;
        }
        #endregion

        #region Events
        public event SelectionChangedEventHandler TextureMagFilterChanged;
        public event SelectionChangedEventHandler TextureMinFilterChanged;
        public event SelectionChangedEventHandler TextureWrapSChanged;
        public event SelectionChangedEventHandler TextureWrapTChanged;
        public event SelectionChangedEventHandler TextureParametersChanged;

        private void ComboBox_TextureMagFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            TextureMagFilterChanged?.Invoke(sender, e);
            OnTextureParametersChanged(sender, e);
        }

        private void ComboBox_TextureMinFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            TextureMinFilterChanged?.Invoke(sender, e);
            OnTextureParametersChanged(sender, e);
        }

        private void ComboBox_TextureWrapS_Changed(object sender, SelectionChangedEventArgs e)
        {
            TextureWrapSChanged?.Invoke(sender, e);
            OnTextureParametersChanged(sender, e);
        }

        private void ComboBox_TextureWrapT_Changed(object sender, SelectionChangedEventArgs e)
        {
            TextureWrapTChanged?.Invoke(sender, e);
            OnTextureParametersChanged(sender, e);
        }

        private void OnTextureParametersChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isProgrammaticControlEvent)
            {
                TextureParametersChanged?.Invoke(sender, e);
            }
        }
        #endregion

        public void SetDefaultParameters()
        {
            isProgrammaticControlEvent = true;

            UseMipMaps = false;
            TextureMagFilter = TextureMagFilter.Linear;
            TextureMinFilter = TextureMinFilter.Linear;
            TextureWrapS = TextureWrapMode.Repeat;
            TextureWrapT = TextureWrapMode.Repeat;

            isProgrammaticControlEvent = false;
        }
    }
}