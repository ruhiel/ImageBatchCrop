using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace ImageBatchCrop
{
    public class FileNameConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return value is string path
                ? Path.GetFileName(path)
                : string.Empty;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
