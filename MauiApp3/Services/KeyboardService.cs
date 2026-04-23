namespace MauiApp3.Services;

public static class KeyboardService
{
    public static double KeyboardHeight { get; private set; }

    public static event EventHandler? HeightChanged;

    public static void NotifyHeightChanged(double height)
    {
        KeyboardHeight = height;
        HeightChanged?.Invoke(null, EventArgs.Empty);
    }
}
