using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CleaningRepo
{
    [ValueConversion(typeof(List<string>), typeof(string))]

    class ListToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable)
            {
                return string.Join(Environment.NewLine, ((IEnumerable)value).OfType<string>().ToArray());
            }
            return "no messages yet";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
