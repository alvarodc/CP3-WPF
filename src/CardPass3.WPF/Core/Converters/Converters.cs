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
                ReaderConnectionState.Idle            => "Inactivo",
                ReaderConnectionState.Connecting      => "Conectando…",
                ReaderConnectionState.TcpConnected    => "TCP OK",
                ReaderConnectionState.ReaderConnected => "Conectado",
                ReaderConnectionState.Failed          => "Error",
                ReaderConnectionState.Disconnected    => "Desconectado",
                _                                     => "Desconocido"
            };
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class ReaderStateToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush ReaderOk   = new(Color.FromRgb(0x43, 0xA0, 0x47)); // verde
        private static readonly SolidColorBrush TcpOk      = new(Color.FromRgb(0x02, 0x88, 0xD1)); // azul
        private static readonly SolidColorBrush Connecting = new(Color.FromRgb(0xF5, 0x7C, 0x00)); // naranja
        private static readonly SolidColorBrush Failed     = new(Color.FromRgb(0xC6, 0x28, 0x28)); // rojo
        private static readonly SolidColorBrush Idle       = new(Color.FromRgb(0x9E, 0x9E, 0x9E)); // gris

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (ReaderConnectionState)value switch
            {
                ReaderConnectionState.ReaderConnected => ReaderOk,
                ReaderConnectionState.TcpConnected    => TcpOk,
                ReaderConnectionState.Connecting      => Connecting,
                ReaderConnectionState.Failed          => Failed,
                _                                     => Idle
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

    /// <summary>Returns Visible if the string is non-empty, Collapsed otherwise. Used for validation messages.</summary>
    public class StringNotEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is string s && !string.IsNullOrEmpty(s) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
