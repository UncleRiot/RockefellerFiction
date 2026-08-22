using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;

namespace RockefellerFiction;

public static class WindowBehavior
{
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;
    private const int DwmwaUseImmersiveDarkMode = 20;

    private static readonly string WindowSizesFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RockefellerFiction",
        "window-sizes.json");

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
        window.SourceInitialized += (_, _) =>
        {
            ApplyDarkTitleBarCore(window);
            RestoreWindowSize(window);
        };

        window.Closed += (_, _) => SaveWindowSize(window);

        if (new WindowInteropHelper(window).Handle != IntPtr.Zero)
        {
            ApplyDarkTitleBarCore(window);
            RestoreWindowSize(window);
        }
    }

    private static void OnStartupLocationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Window window && e.NewValue is WindowStartupLocation location)
        {
            window.WindowStartupLocation = location;
        }
    }

    private static void RestoreWindowSize(Window window)
    {
        Dictionary<string, WindowSizeState> states = LoadWindowSizes();

        string key = window.GetType().FullName ?? window.GetType().Name;

        if (!states.TryGetValue(key, out WindowSizeState? state))
            return;

        if (state.Width > 0 && double.IsFinite(state.Width))
            window.Width = Clamp(state.Width, window.MinWidth, window.MaxWidth);

        if (state.Height > 0 && double.IsFinite(state.Height))
            window.Height = Clamp(state.Height, window.MinHeight, window.MaxHeight);

        if (state.IsMaximized && window.ResizeMode != ResizeMode.NoResize)
            window.WindowState = WindowState.Maximized;
    }

    private static void SaveWindowSize(Window window)
    {
        Dictionary<string, WindowSizeState> states = LoadWindowSizes();

        Rect bounds = window.WindowState == WindowState.Normal
            ? new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight)
            : window.RestoreBounds;

        if (bounds.Width <= 0 ||
            bounds.Height <= 0 ||
            !double.IsFinite(bounds.Width) ||
            !double.IsFinite(bounds.Height))
        {
            return;
        }

        string key = window.GetType().FullName ?? window.GetType().Name;

        states[key] = new WindowSizeState
        {
            Width = bounds.Width,
            Height = bounds.Height,
            IsMaximized = window.WindowState == WindowState.Maximized
        };

        string? directory = Path.GetDirectoryName(WindowSizesFilePath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        string json = JsonSerializer.Serialize(
            states,
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(WindowSizesFilePath, json);
    }

    private static Dictionary<string, WindowSizeState> LoadWindowSizes()
    {
        try
        {
            if (!File.Exists(WindowSizesFilePath))
                return new Dictionary<string, WindowSizeState>();

            string json = File.ReadAllText(WindowSizesFilePath);

            return JsonSerializer.Deserialize<Dictionary<string, WindowSizeState>>(json)
                   ?? new Dictionary<string, WindowSizeState>();
        }
        catch
        {
            return new Dictionary<string, WindowSizeState>();
        }
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        double result = Math.Max(value, minimum);

        if (!double.IsPositiveInfinity(maximum))
            result = Math.Min(result, maximum);

        return result;
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

    private sealed class WindowSizeState
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsMaximized { get; set; }
    }
}
