using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace Orts.TimetableEditor.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var boolValue = (bool)value;
                if (boolValue) return Visibility.Visible;
                else return Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (((Visibility)value).Equals(Visibility.Collapsed)) return false;
                else return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }

    public class BooleanToVisibilityConverterHidden : IValueConverter
    {
        public Object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var boolValue = (bool)value;
                if (boolValue) return Visibility.Visible;
                else return Visibility.Hidden;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (((Visibility)value).Equals(Visibility.Hidden)) return false;
                else return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
