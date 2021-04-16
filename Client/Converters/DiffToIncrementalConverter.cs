using System;
using System.Globalization;
using System.Windows.Data;

namespace Client.Converters
{
    public class DiffToIncrementalConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( value is bool)
                return !(bool)parameter;

            return false;
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( value is bool isIncremental )
                return !isIncremental;

            return false;
        }
    }
}
