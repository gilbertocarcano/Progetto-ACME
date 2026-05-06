using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace AcmeUI.Converters
{
    public class FloatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is float f)
                return f.ToString(CultureInfo.InvariantCulture);

            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string s = value?.ToString() ?? "0";

            // accetta sia virgola che punto
            s = s.Replace(',', '.');

            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                return f;

            return 0f;
        }
    }
}

