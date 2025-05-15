using System;
using System.Windows;
using System.Windows.Controls;

namespace ShaderPluginGUI.Controls
{
    public partial class IconPresenter : UserControl
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon),
            typeof(UIElement), typeof(IconPresenter));

        public static readonly DependencyProperty IconWidthProperty = DependencyProperty.Register(nameof(IconWidth),
            typeof(double), typeof(IconPresenter), new PropertyMetadata(24.0));

        public static readonly DependencyProperty IconHeightProperty = DependencyProperty.Register(nameof(IconHeight),
            typeof(double), typeof(IconPresenter), new PropertyMetadata(24.0));

        public IconPresenter()
        {
            InitializeComponent();
        }

        public UIElement Icon
        {
            get { return (UIElement)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public double IconWidth
        {
            get { return (double)GetValue(IconWidthProperty); }
            set { SetValue(IconWidthProperty, value); }
        }

        public double IconHeight
        {
            get { return (double)GetValue(IconHeightProperty); }
            set { SetValue(IconHeightProperty, value); }
        }

        public void SetIconFromResource(string StaticResourceKey)
        {
            if (String.IsNullOrEmpty(StaticResourceKey))
            {
                Icon = null;
                return;
            }

            Icon = TryFindResource(StaticResourceKey) as UIElement;
        }
    }
}
