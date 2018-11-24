using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Airswipe.WinRT.Core.Data
{
    public class ConnectionTypeConverter : IValueConverter
    {
        public ConnectionTypeConverter() { }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (value == null) ? null : Enum.GetValues(typeof(ConnectionType)).Cast<Enum>().Select(v => v.ToString());
            //return new List<string> {"abe", "kat"};
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
