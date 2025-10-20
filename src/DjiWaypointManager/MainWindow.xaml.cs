using DjiWaypointManager.Models;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Wpf;
using System.IO.Compression;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;
using IOSearchOption = System.IO.SearchOption;
using DjiWaypointManager.Services;

namespace DjiWaypointManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Mission _mission = new();
        private WebView2? _webView;
        private bool _isWebViewReady = false;
        private DjiRemoteControlDetector? _djiDetector;
        private DjiRemoteControlDevice? _selectedDevice;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize WebView2
                await InitializeWebViewAsync();
                
                // Initialize DJI device detection
                InitializeDjiDetection();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _djiDetector?.Dispose();
        }

        private async Task InitializeWebViewAsync()
        {
            // Create WebView2 control programmatically
            _webView = new WebView2();
            WebViewContainer.Child = _webView;
            
            await _webView.EnsureCoreWebView2Async();
            
            // Get the path to the HTML file
            var htmlPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "map.html");
            
            if (!IOFile.Exists(htmlPath))
            {
                MessageBox.Show($"Error initializing WebView2: Could not load map.html", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Add navigation completed handler
            _webView.NavigationCompleted += WebView_NavigationCompleted;
            
            _webView.Source = new Uri(htmlPath);
        }

        private void InitializeDjiDetection()
        {
            try
            {
                _djiDetector = new DjiRemoteControlDetector();
                _djiDetector.RemoteControlConnected += OnDjiDeviceConnected;
                _djiDetector.RemoteControlDisconnected += OnDjiDeviceDisconnected;
                _djiDetector.StartMonitoring();

                UpdateDeviceStatus("Scanning for DJI devices...");
            }
            catch (Exception ex)
            {
                UpdateDeviceStatus($"Error initializing device detection: {ex.Message}");
            }
        }

        private void OnDjiDeviceConnected(DjiRemoteControlDevice device)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateDeviceComboBox();
                UpdateDeviceStatus($"DJI device connected: {device.Name}");
            });
        }

        private void OnDjiDeviceDisconnected(DjiRemoteControlDevice device)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateDeviceComboBox();
                if (_selectedDevice?.DeviceId == device.DeviceId)
                {
                    _selectedDevice = null;
                    LoadFromDeviceButton.IsEnabled = false;
                    DeviceFilesListBox.Items.Clear();
                }
                UpdateDeviceStatus($"DJI device disconnected: {device.Name}");
            });
        }

        private void UpdateDeviceComboBox()
        {
            var devices = _djiDetector?.GetConnectedDevices() ?? new List<DjiRemoteControlDevice>();
            DjiDevicesComboBox.Items.Clear();
            
            foreach (var device in devices)
            {
                DjiDevicesComboBox.Items.Add(device);
            }

            if (devices.Count > 0)
            {
                DjiDevicesComboBox.SelectedIndex = 0;
            }
        }

        private void UpdateDeviceStatus(string status)
        {
            DeviceStatusText.Text = status;
        }

        private async void WebView_NavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                _isWebViewReady = true;
                System.Diagnostics.Debug.WriteLine("WebView navigation completed successfully");

                // If we already have a mission loaded, render it now
                if (_mission.Waypoints.Any() || _mission.Pois.Any())
                {
                    await RenderMissionOnMapAsync();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"WebView navigation failed: {e.WebErrorStatus}");
            }
        }

        private async Task RenderMissionOnMapAsync()
        {
            if (_webView?.CoreWebView2 == null || !_isWebViewReady)
            {
                System.Diagnostics.Debug.WriteLine("WebView not ready for rendering mission");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"Rendering mission: {_mission.Waypoints.Count} waypoints, {_mission.Pois.Count} POIs");

                // Debug waypoint UseStraightLine values
                System.Diagnostics.Debug.WriteLine("=== WAYPOINT UseStraightLine VALUES ===");
                foreach (var wp in _mission.Waypoints.Take(5)) // Show first 5 waypoints
                {
                    System.Diagnostics.Debug.WriteLine($"Waypoint {wp.Index}: UseStraightLine = {wp.UseStraightLine}");
                }

                // Create waypoint data for JavaScript (including POI references)
                var waypointsForJs = _mission.Waypoints.Select(w => new
                {
                    w.Index,
                    w.Lat,
                    w.Lon,
                    w.ExecuteHeight,
                    w.Speed,
                    w.UseStraightLine,
                    w.TurnDampingDist,
                    Actions = w.Actions.Select(a => new { a.Func }).ToList(),
                    // Include POI data if this waypoint references a POI
                    poiIndex = w.PoiIndex.HasValue 
                        ? _mission.Pois.Where(p => p.Index == w.PoiIndex.Value)
                                      .Select(p => p.Index)
                                      .FirstOrDefault()
                        : -1
                }).ToList();

                // Create standalone POI data for JavaScript
                var poisForJs = _mission.Pois.Select(p => new
                {
                    p.Index,
                    p.Lat,
                    p.Lon,
                    p.Alt
                }).ToList();

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };
                
                var waypointsJson = JsonSerializer.Serialize(waypointsForJs, options);
                var poisJson = JsonSerializer.Serialize(poisForJs, options);
                var showPois = ShowPoisCheckbox?.IsChecked == true;
                
                System.Diagnostics.Debug.WriteLine($"Waypoints JSON: {waypointsJson.Substring(0, Math.Min(200, waypointsJson.Length))}...");
                System.Diagnostics.Debug.WriteLine($"Straight line segments: {_mission.Waypoints.Count(w => w.UseStraightLine)}");
                System.Diagnostics.Debug.WriteLine($"Curved segments: {_mission.Waypoints.Count(w => !w.UseStraightLine)}");
                
                // Enhanced JavaScript to handle both waypoints and standalone POIs
                var script = $@"
                try {{
                    console.log('=== Rendering Mission ===');
                    var waypointsData = {waypointsJson};
                    var poisData = {poisJson};
                    var showPois = {showPois.ToString().ToLower()};
                    
                    console.log('Waypoints count:', waypointsData.length);
                    console.log('POIs count:', poisData.length);
                    console.log('First waypoint:', waypointsData[0]);
                    
                    // Debug waypoint segments and UseStraightLine values
                    console.log('=== WAYPOINT UseStraightLine DEBUG ===');
                    for (let i = 0; i < Math.min(5, waypointsData.length); i++) {{
                        console.log('Waypoint ' + waypointsData[i].index + ':', {{
                            useStraightLine: waypointsData[i].useStraightLine,
                            lat: waypointsData[i].lat,
                            lon: waypointsData[i].lon
                        }});
                    }}
                    
                    window.showMission(JSON.stringify(waypointsData), JSON.stringify(poisData), showPois);
                    console.log('=== Mission Rendering Complete ===');
                }} catch(e) {{
                    console.error('❌ Error rendering mission:', e);
                    console.error('Stack trace:', e.stack);
                }}";
                
                await _webView.CoreWebView2.ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine("Mission render script executed");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error rendering mission on map: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                System.Diagnostics.Debug.WriteLine($"RenderMissionOnMapAsync error: {ex}");
            }
        }

        private async void OpenKmz_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "DJI missions (*.kmz;*.zip)|*.kmz;*.zip|All files (*.*)|*.*",
                Title = "Select DJI Mission File"
            };
            
            if (dlg.ShowDialog() != true) return;

            await LoadMissionFromFile(dlg.FileName);
        }

        private async Task LoadMissionFromFile(string filePath)
        {
            try
            {
                string tempDir = IOPath.Combine(IOPath.GetTempPath(), "DjiMission_" + Guid.NewGuid().ToString("N"));
                IODirectory.CreateDirectory(tempDir);
                
                ZipFile.ExtractToDirectory(filePath, tempDir);

                // Find waylines.wpml file
                string? wpml = IODirectory.GetFiles(tempDir, "waylines.wpml", IOSearchOption.AllDirectories).FirstOrDefault()
                            ?? IODirectory.GetFiles(tempDir, "*.wpml", IOSearchOption.AllDirectories).FirstOrDefault()
                            ?? IODirectory.GetFiles(tempDir, "*.kml", IOSearchOption.AllDirectories).FirstOrDefault();

                if (wpml is null)
                {
                    MessageBox.Show("No waylines.wpml or KML file found in the archive.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Parse the mission
                _mission = MissionParser.ParseMission(wpml);
                
                // Update UI
                UpdateMissionUI();

                // Render on map if WebView is ready
                if (_isWebViewReady)
                {
                    await RenderMissionOnMapAsync();
                }

                // Cleanup temp directory
                try { IODirectory.Delete(tempDir, true); } catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading mission: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ScanDjiDevices_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _djiDetector?.Dispose();
                InitializeDjiDetection();
                UpdateDeviceStatus("Rescanning for DJI devices...");
            }
            catch (Exception ex)
            {
                UpdateDeviceStatus($"Error scanning devices: {ex.Message}");
            }
        }

        private void DjiDevicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedDevice = DjiDevicesComboBox.SelectedItem as DjiRemoteControlDevice;
            LoadFromDeviceButton.IsEnabled = _selectedDevice != null;
            
            if (_selectedDevice != null)
            {
                RefreshDeviceFiles();
            }
        }

        private async void LoadFromDevice_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDevice == null) return;

            var selectedFile = DeviceFilesListBox.SelectedItem as string;
            if (selectedFile == null)
            {
                MessageBox.Show("Please select a waypoint file from the device.", "No File Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                UpdateDeviceStatus("Downloading waypoint file...");

                var tempDir = IOPath.Combine(IOPath.GetTempPath(), "DjiDeviceDownload");
                var localFile = await _selectedDevice.DownloadWaypointFile(selectedFile, tempDir);

                if (localFile != null)
                {
                    await LoadMissionFromFile(localFile);
                    UpdateDeviceStatus($"Mission loaded from device: {IOPath.GetFileName(selectedFile)}");
                }
                else
                {
                    UpdateDeviceStatus("Failed to download file from device");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading mission from device: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateDeviceStatus($"Error: {ex.Message}");
            }
        }

        private void RefreshDeviceFiles()
        {
            if (_selectedDevice == null) return;

            try
            {
                var files = _selectedDevice.GetWaypointFiles();
                DeviceFilesListBox.Items.Clear();
                
                foreach (var file in files)
                {
                    DeviceFilesListBox.Items.Add(file);
                }

                UpdateDeviceStatus($"Found {files.Count} waypoint files on device");
            }
            catch (Exception ex)
            {
                UpdateDeviceStatus($"Error reading device files: {ex.Message}");
            }
        }

        private void DeviceFilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Enable load button when a file is selected
            LoadFromDeviceButton.IsEnabled = DeviceFilesListBox.SelectedItem != null && _selectedDevice != null;
        }

        private void RefreshDeviceFiles_Click(object sender, RoutedEventArgs e)
        {
            RefreshDeviceFiles();
        }

        // ... existing code for mission display and map interaction ...

        private void UpdateMissionUI()
        {
            // Update mission info
            MissionInfo.Text = $"Mission loaded: {_mission.Waypoints.Count} waypoints, {_mission.Pois.Count} POIs";
            
            // Update Mission Info tab
            UpdateMissionInfoTab();
            
            // Bind data to grids
            WaypointsGrid.ItemsSource = _mission.Waypoints;
            PoisGrid.ItemsSource = _mission.Pois;
            
            // Select mission info tab first when loading a new mission
            if (_mission.Waypoints.Any() || _mission.Pois.Any())
            {
                DataTabControl.SelectedItem = MissionInfoTab;
            }
        }

        private void UpdateMissionInfoTab()
        {
            // Update drone information
            DroneModelText.Text = _mission.Config.DroneInfo.DroneModel;
            DroneCodesText.Text = $"Enum: {_mission.Config.DroneInfo.DroneEnumValue}, Sub: {_mission.Config.DroneInfo.DroneSubEnumValue}";
            
            // Update mission configuration
            FlyModeText.Text = FormatConfigValue(_mission.Config.FlyToWaylineMode);
            FinishActionText.Text = FormatConfigValue(_mission.Config.FinishAction);
            RCLostActionText.Text = FormatConfigValue(_mission.Config.ExecuteRCLostAction);
            
            // Update speeds and counts
            GlobalSpeedText.Text = _mission.Config.GlobalTransitionalSpeed > 0 
                ? $"{_mission.Config.GlobalTransitionalSpeed:F1} m/s" 
                : "Not specified";
            
            TotalWaypointsText.Text = _mission.Waypoints.Count.ToString();
            TotalPoisText.Text = _mission.Pois.Count.ToString();
        }

        private string FormatConfigValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "Not specified";
            
            // Convert camelCase to readable format
            return value switch
            {
                "safely" => "Safely",
                "goHome" => "Go Home",
                "hover" => "Hover",
                "land" => "Land",
                "executeLostAction" => "Execute Lost Action",
                "goBack" => "Go Back",
                "continue" => "Continue",
                _ => char.ToUpper(value[0]) + value.Substring(1)
            };
        }

        private async void WaypointsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (WaypointsGrid.SelectedItem is Waypoint wp && _webView?.CoreWebView2 != null && _isWebViewReady)
                {
                    var lat = wp.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    var lon = wp.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    var script = $"try {{ window.highlightWaypoint({lat}, {lon}, {wp.Index}); }} catch(e) {{ console.error('Error highlighting waypoint:', e); }}";
                    await _webView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error highlighting waypoint: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void PoisGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (PoisGrid.SelectedItem is Poi poi && _webView?.CoreWebView2 != null && _isWebViewReady)
                {
                    var lat = poi.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    var lon = poi.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    var script = $"try {{ window.highlightPoi({lat}, {lon}, {poi.Index}); }} catch(e) {{ console.error('Error highlighting POI:', e); }}";
                    await _webView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error highlighting POI: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void FitToMission_Click(object sender, RoutedEventArgs e)
        {
            if (_webView?.CoreWebView2 != null && _isWebViewReady)
            {
                try
                {
                    var script = "try { window.fitToMission(); } catch(e) { console.error('Error fitting to mission:', e); }";
                    await _webView.CoreWebView2.ExecuteScriptAsync(script);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error fitting map to mission: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async void ShowPois_Changed(object sender, RoutedEventArgs e)
        {
            if (_isWebViewReady)
            {
                await RenderMissionOnMapAsync();
            }
        }
    }
}