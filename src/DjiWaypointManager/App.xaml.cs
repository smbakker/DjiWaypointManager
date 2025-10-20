using DjiWaypointManager.Views;
using System.Windows;

namespace DjiWaypointManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private SplashWindow? _splashWindow;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            // Show splash screen
            _splashWindow = new SplashWindow();
            _splashWindow.Show();

            // Simulate loading process
            await SimulateLoadingProcess();

            // Create and show main window
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;

            // Close splash screen and show main window
            _splashWindow.CloseSplash();
            mainWindow.Show();
        }

        private async Task SimulateLoadingProcess()
        {
            if (_splashWindow == null) return;

            // Simulate various loading stages
            var loadingSteps = new[]
            {
                ("Initializing application...", 500),
                ("Loading user interface...", 300),
                ("Setting up device detection...", 600),
                ("Preparing map components...", 400),
                ("Loading resources...", 200),
                ("Ready to launch!", 300)
            };

            for (int i = 0; i < loadingSteps.Length; i++)
            {
                var (message, delay) = loadingSteps[i];
                
                _splashWindow.UpdateStatus(message);
                _splashWindow.SetProgress((double)(i + 1) / loadingSteps.Length);
                
                await Task.Delay(delay);
            }

            // Final delay to show "Ready to launch!" message
            await Task.Delay(500);
        }
    }
}
