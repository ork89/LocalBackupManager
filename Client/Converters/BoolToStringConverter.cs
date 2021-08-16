using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace Client.Converters
{
    public class BoolToStringConverter : IValueConverter
    {
        public string TrueValue { get; set; }
        public string FalseValue { get; set; }

        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            return value == null ? FalseValue : ( (bool)value ? TrueValue : FalseValue );
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            return value != null && EqualityComparer<string>.Default.Equals( (string)value, TrueValue );
        }
    }
}
