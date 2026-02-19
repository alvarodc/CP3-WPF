using CardPass3.WPF.Services.Readers;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CardPass3.WPF.Core.Converters
{
    public class ReaderStateToLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (ReaderConnectionState)value switch
            {
                ReaderConnectionState.Connected    => "Conectado",
                ReaderConnectionState.Connecting   => "Conectando…",
                ReaderConnectionState.Failed       => "Error",
                ReaderConnectionState.Disconnected => "Desconectado",
                _                                  => "Inactivo"
            };
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class ReaderStateToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush Connected  = new(Color.FromRgb(0x43, 0xA0, 0x47));
        private static readonly SolidColorBrush Connecting = new(Color.FromRgb(0xF5, 0x7C, 0x00));
        private static readonly SolidColorBrush Failed     = new(Color.FromRgb(0xC6, 0x28, 0x28));
        private static readonly SolidColorBrush Idle       = new(Color.FromRgb(0x9E, 0x9E, 0x9E));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (ReaderConnectionState)value switch
            {
                ReaderConnectionState.Connected  => Connected,
                ReaderConnectionState.Connecting => Connecting,
                ReaderConnectionState.Failed     => Failed,
                _                                => Idle
            };
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class EqualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.ToString() == parameter?.ToString();
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
