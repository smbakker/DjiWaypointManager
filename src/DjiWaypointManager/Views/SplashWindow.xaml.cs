using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DjiWaypointManager.Views
{
    /// <summary>
    /// Interaction logic for SplashWindow.xaml
    /// </summary>
    public partial class SplashWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private readonly string[] _loadingMessages = 
        {
            "Initializing application...",
            "Loading user interface...",
            "Setting up device detection...",
            "Preparing map components...",
            "Loading resources...",
            "Ready to launch!"
        };
        
        private int _messageIndex = 0;

        public SplashWindow()
        {
            InitializeComponent();
            
            // Check if splash.png exists, show fallback if not
            CheckSplashImage();
            
            // Setup loading animation
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(800)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void CheckSplashImage()
        {
            try
            {
                // Try different possible locations for the splash image
                var possiblePaths = new[]
                {
                    "splash.png",
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "splash.png"),
                    Path.Combine(Environment.CurrentDirectory, "splash.png")
                };

                string? foundPath = null;
                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        foundPath = path;
                        break;
                    }
                }

                if (foundPath != null)
                {
                    // Load the splash image
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(foundPath, UriKind.RelativeOrAbsolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    
                    SplashImage.Source = bitmap;
                    SplashImage.Visibility = Visibility.Visible;
                    FallbackContent.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Show fallback content if splash.png doesn't exist
                    ShowFallbackContent();
                }
            }
            catch (Exception ex)
            {
                // Show fallback content if there's any error loading the image
                System.Diagnostics.Debug.WriteLine($"Could not load splash image: {ex.Message}");
                ShowFallbackContent();
            }
        }

        private void ShowFallbackContent()
        {
            SplashImage.Visibility = Visibility.Collapsed;
            FallbackContent.Visibility = Visibility.Visible;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_messageIndex < _loadingMessages.Length)
            {
                LoadingText.Text = _loadingMessages[_messageIndex];
                _messageIndex++;
            }
            else
            {
                _timer.Stop();
                // Close splash screen and show main window
                CloseSplash();
            }
        }

        public void CloseSplash()
        {
            _timer?.Stop();
            
            // Fade out animation
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(500));
            fadeOut.Completed += (s, e) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }

        public void UpdateStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LoadingText.Text = message;
            });
        }

        public void SetProgress(double progress)
        {
            Dispatcher.Invoke(() =>
            {
                LoadingProgress.IsIndeterminate = false;
                LoadingProgress.Value = progress * 100;
            });
        }
    }
}