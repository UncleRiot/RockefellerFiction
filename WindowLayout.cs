using System.Windows;

namespace RockefellerFiction;

public static class WindowLayout
{
    public const double Width = 1180;
    public const double Height = 820;
    public const double MinWidth = 980;
    public const double MinHeight = 680;

    public static void ApplyMainWindow(Window window)
    {
        ApplyCommon(window);
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    public static void ApplyResultsWindow(Window window)
    {
        ApplyCommon(window);
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }

    private static void ApplyCommon(Window window)
    {
        window.Width = Width;
        window.Height = Height;
        window.MinWidth = MinWidth;
        window.MinHeight = MinHeight;
    }
}
