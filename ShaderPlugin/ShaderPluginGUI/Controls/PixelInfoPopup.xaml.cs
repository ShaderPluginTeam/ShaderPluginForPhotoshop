using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using OpenTK;

using Point = System.Drawing.Point;

namespace ShaderPluginGUI.Controls
{
    public partial class PixelInfoPopup : Popup
    {
        private DispatcherTimer UIUpdateTimer;

        // Current mouse location on GLControl (used only as temporal storage for render loop)
        public Point MouseLocation;

        public ColorRGBA PixelColor;
        public Vector2 PixelUV;
        public Vector2 PixelPosition;

        public PixelInfoPopup()
        {
            InitializeComponent();

            UIUpdateTimer = new DispatcherTimer();
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(1);
            UIUpdateTimer.Tick += UpdateUIElements;
        }

        ~PixelInfoPopup()
        {
            UIUpdateTimer?.Stop();
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            UIUpdateTimer?.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            UIUpdateTimer?.Stop();
            base.OnClosed(e);
        }

        private void UpdateUIElements(object sender, EventArgs e)
        {
            textBlock_ColorFloat_R.Text = PixelColor.R.ToString();
            textBlock_ColorFloat_G.Text = PixelColor.G.ToString();
            textBlock_ColorFloat_B.Text = PixelColor.B.ToString();
            textBlock_ColorFloat_A.Text = PixelColor.A.ToString();

            textBlock_ColorByte_R.Text = PixelColor.RByte.ToString();
            textBlock_ColorByte_G.Text = PixelColor.GByte.ToString();
            textBlock_ColorByte_B.Text = PixelColor.BByte.ToString();
            textBlock_ColorByte_A.Text = PixelColor.AByte.ToString();

            textBlock_UV_X.Text = PixelUV.X.ToString();
            textBlock_UV_Y.Text = PixelUV.Y.ToString();

            textBlock_Pixel_X.Text = ((int)PixelPosition.X).ToString();
            textBlock_Pixel_Y.Text = ((int)PixelPosition.Y).ToString();
        }
    }
}
