using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Graphics;

namespace MauiApp3
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
        // adjustResize makes the activity resize when the keyboard appears,
        // so the input fields stay visible and the main area shrinks properly
        WindowSoftInputMode = SoftInput.AdjustResize,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // MAGIC FIX for .NET MAUI Android AdjustResize:
            // This forces Android to fit the content within system windows (not edge-to-edge).
            // Without this, AdjustResize may fail when the navigation bar is hidden or transparent.
            if (Window != null)
            {
                AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(Window, true);
            }

            // Set status bar color to match the app's dark blue theme
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                Window?.SetStatusBarColor(Android.Graphics.Color.ParseColor("#16213e"));
            }
        }
    }
}
