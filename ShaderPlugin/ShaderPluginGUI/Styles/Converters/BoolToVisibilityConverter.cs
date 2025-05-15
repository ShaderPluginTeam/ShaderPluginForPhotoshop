using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ShaderPluginGUI.Styles.Converters
{
    /// <summary>
    /// Represents the converter that converts Boolean values to and from System.Windows.Visibility enumeration values.
    /// Same as default WPF implementation, but allow to use Inversion.
    /// </summary>
    [Localizability(LocalizationCategory.NeverLocalize)]
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a Boolean value to a System.Windows.Visibility enumeration value.
        /// </summary>
        /// <param name="value">The Boolean value to convert. This value can be a standard Boolean value or a nullable Boolean value.</param>
        /// <param name="targetType">This parameter is not used.</param>
        /// <param name="parameter">This parameter is not used.</param>
        /// <param name="culture">This parameter is not used.</param>
        /// <returns>System.Windows.Visibility.Visible if value is true; otherwise, System.Windows.Visibility.Collapsed.</returns>
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool bVisibility)
            {
                return (IsInverted ^ bVisibility) ? Visibility.Visible : Visibility.Hidden;
            }
            else if (value is bool?)
            {
                bool? bNullableVisibility = (bool?)value;
                return (IsInverted ^ (bNullableVisibility.HasValue && bNullableVisibility.Value)) ? Visibility.Visible : Visibility.Hidden;
            }

            return Visibility.Hidden;
        }

        /// <summary>
        /// Converts a System.Windows.Visibility enumeration value to a Boolean value.
        /// </summary>
        /// <param name="value">A System.Windows.Visibility enumeration value.</param>
        /// <param name="targetType">This parameter is not used.</param>
        /// <param name="parameter">This parameter is not used.</param>
        /// <param name="culture">This parameter is not used.</param>
        /// <returns>true if value is System.Windows.Visibility.Visible; otherwise, false.</returns>
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return IsInverted ^ (visibility == Visibility.Visible);
            }

            return false;
        }

        // Invert conversion.
        public bool IsInverted {  get; set; }
    }
}
