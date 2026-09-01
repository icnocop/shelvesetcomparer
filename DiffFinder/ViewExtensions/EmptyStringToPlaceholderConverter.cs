namespace DiffFinder
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// Converter which replaces an empty value with the text given as the converter parameter.
    /// Used by the shelveset owner drop down lists to render the "no user selected" entry, which is an
    /// empty string, as readable text instead of a blank row.
    /// </summary>
    public class EmptyStringToPlaceholderConverter : IValueConverter
    {
        /// <summary>
        /// Returns the value, or the converter parameter when the value is empty.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">The text to display instead of an empty value</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>The text to display</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DependencyProperty.UnsetValue || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return parameter ?? string.Empty;
            }

            return value;
        }

        /// <summary>
        /// Not supported, the conversion is display only.
        /// </summary>
        /// <param name="value">The value to convert back</param>
        /// <param name="targetType">The type to convert back to</param>
        /// <param name="parameter">The converter parameter</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>Never returns, always throws</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
