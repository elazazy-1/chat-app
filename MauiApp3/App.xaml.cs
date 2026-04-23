using Microsoft.Maui;

namespace MauiApp3
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            UserAppTheme = AppTheme.Light;

            // Fix mobile keyboard pushing the whole screen up (Force AdjustResize)
            Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.Application.SetWindowSoftInputModeAdjust(
                this,
                Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.WindowSoftInputModeAdjust.Resize
            );

            // Capture unhandled .NET exceptions before Android wraps them
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                System.Diagnostics.Debug.WriteLine($"[FATAL] Unhandled: {ex?.GetType().Name}: {ex?.Message}");
                System.Diagnostics.Debug.WriteLine($"[FATAL] Stack: {ex?.StackTrace}");
                if (ex?.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[FATAL] Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"[FATAL] Inner Stack: {ex.InnerException.StackTrace}");
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[FATAL] UnobservedTask: {e.Exception}");
                e.SetObserved();
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            window.FlowDirection = Microsoft.Maui.FlowDirection.LeftToRight;

#if WINDOWS
            // Set a phone-friendly portrait size for the Windows desktop app
            window.Width = 420;
            window.Height = 820;
            window.MinimumWidth = 380;
            window.MinimumHeight = 600;
#endif

            return window;
        }
    }
}