using System.Windows.Input;
using System.Windows;

namespace ShaderPluginGUI.CommandBehavior
{
    public static class MouseUpCommandBehavior
    {
        public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
            "Command",
            typeof(ICommand),
            typeof(MouseUpCommandBehavior),
            new PropertyMetadata(null, OnCommandChanged));

        public static ICommand GetCommand(DependencyObject obj) => (ICommand)obj.GetValue(CommandProperty);
        public static void SetCommand(DependencyObject obj, ICommand value) => obj.SetValue(CommandProperty, value);

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if (e.NewValue != null)
                {
                    element.MouseUp += Element_MouseUp;
                }
                else
                {
                    element.MouseUp -= Element_MouseUp;
                }
            }
        }

        private static void Element_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is UIElement element)
            {
                ICommand command = GetCommand(element);
                if (command is RoutedCommand routedCommand)
                {
                    if (routedCommand.CanExecute(e, element))
                        routedCommand.Execute(e, element);
                }
                else
                {
                    if (command != null && command.CanExecute(e))
                        command.Execute(e);
                }
            }
        }
    }
}
