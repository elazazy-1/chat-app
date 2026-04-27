namespace MauiApp3.Services;

/// <summary>
/// Provides information about the software keyboard's height and state.
/// </summary>
public static class KeyboardService
{
    /// <summary>Gets the current height of the software keyboard.</summary>
    public static double KeyboardHeight { get; private set; }

    /// <summary>Event raised when the keyboard height changes.</summary>
    public static event EventHandler? HeightChanged;

    /// <summary>
    /// Notifies subscribers that the keyboard height has changed.
    /// </summary>
    /// <param name="height">The new keyboard height.</param>
    public static void NotifyHeightChanged(double height)
    {
        // Store the height statically for polling
        KeyboardHeight = height;
        
        // Notify all active event listeners (like chat views) so they can adjust their padding dynamically
        HeightChanged?.Invoke(null, EventArgs.Empty);
    }
}
