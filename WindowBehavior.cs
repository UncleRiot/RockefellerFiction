using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace RockefellerFiction;

public static class WindowBehavior
{
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;
    private const int DwmwaUseImmersiveDarkMode = 20;

    public static readonly DependencyProperty StartupLocationProperty =
        DependencyProperty.RegisterAttached(
            "StartupLocation",
            typeof(WindowStartupLocation),
            typeof(WindowBehavior),
            new PropertyMetadata(WindowStartupLocation.Manual, OnStartupLocationChanged));

    public static void SetStartupLocation(DependencyObject element, WindowStartupLocation value)
    {
        element.SetValue(StartupLocationProperty, value);
    }

    public static WindowStartupLocation GetStartupLocation(DependencyObject element)
    {
        return (WindowStartupLocation)element.GetValue(StartupLocationProperty);
    }

    public static void ApplyDarkTitleBar(Window window)
    {
        window.SourceInitialized += (_, _) => ApplyDarkTitleBarCore(window);

        if (new WindowInteropHelper(window).Handle != IntPtr.Zero)
            ApplyDarkTitleBarCore(window);
    }

    private static void OnStartupLocationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Window window && e.NewValue is WindowStartupLocation location)
        {
            window.WindowStartupLocation = location;
        }
    }

    private static void ApplyDarkTitleBarCore(Window window)
    {
        IntPtr handle = new WindowInteropHelper(window).Handle;

        if (handle == IntPtr.Zero)
            return;

        int enabled = 1;
        int result = DwmSetWindowAttribute(
            handle,
            DwmwaUseImmersiveDarkMode,
            ref enabled,
            Marshal.SizeOf<int>());

        if (result != 0)
        {
            DwmSetWindowAttribute(
                handle,
                DwmwaUseImmersiveDarkModeBefore20H1,
                ref enabled,
                Marshal.SizeOf<int>());
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);
}
