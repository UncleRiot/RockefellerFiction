using System.Windows;

namespace RockefellerFiction;

public static class UiLayout
{
    public const double WindowWidth = 1180;
    public const double WindowHeight = 820;
    public const double WindowMinWidth = 980;
    public const double WindowMinHeight = 680;

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
        window.Width = WindowWidth;
        window.Height = WindowHeight;
        window.MinWidth = WindowMinWidth;
        window.MinHeight = WindowMinHeight;
    }
}
