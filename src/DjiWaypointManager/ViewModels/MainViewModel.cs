using DjiWaypointManager.Commands;
using DjiWaypointManager.Models;
using DjiWaypointManager.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Windows;
using System.Windows.Input;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;
using IOSearchOption = System.IO.SearchOption;

namespace DjiWaypointManager.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private DjiRemoteControlDetector? _djiDetector;
        private Mission _mission = new();
        private DjiRemoteControlDevice? _selectedDevice;
        private string _statusBarText = "Ready";
        private bool _isScanningVisible;
        private int _deviceCount;
        private bool _isConnected;
        private string _connectionStatusText = "Disconnected";
        private string _deviceStatusText = "No DJI devices detected";
        private string _missionInfo = "No mission loaded";
        private bool _showPois = true;

        public MainViewModel()
        {
            ConnectedDevices = new ObservableCollection<DjiRemoteControlDevice>();
            DeviceFiles = new ObservableCollection<string>();
            
            InitializeCommands();
            InitializeDjiDetection();
        }

        #region Properties

        public Mission Mission
        {
            get => _mission;
            set => SetProperty(ref _mission, value);
        }

        public ObservableCollection<DjiRemoteControlDevice> ConnectedDevices { get; }
        public ObservableCollection<string> DeviceFiles { get; }

        public DjiRemoteControlDevice? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (SetProperty(ref _selectedDevice, value))
                {
                    OnPropertyChanged(nameof(IsLoadFromDeviceEnabled));
                    if (value != null)
                    {
                        IsConnected = true;
                        ConnectionStatusText = $"Connected: {value.Name}";
                        RefreshDeviceFiles();
                        StatusBarText = $"Selected device: {value.Name}";
                    }
                    else
                    {
                        IsConnected = false;
                        ConnectionStatusText = "Disconnected";
                    }
                }
            }
        }

        public string StatusBarText
        {
            get => _statusBarText;
            set => SetProperty(ref _statusBarText, value);
        }

        public bool IsScanningVisible
        {
            get => _isScanningVisible;
            set => SetProperty(ref _isScanningVisible, value);
        }

        public int DeviceCount
        {
            get => _deviceCount;
            set => SetProperty(ref _deviceCount, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public string ConnectionStatusText
        {
            get => _connectionStatusText;
            set => SetProperty(ref _connectionStatusText, value);
        }

        public string DeviceStatusText
        {
            get => _deviceStatusText;
            set => SetProperty(ref _deviceStatusText, value);
        }

        public string MissionInfo
        {
            get => _missionInfo;
            set => SetProperty(ref _missionInfo, value);
        }

        public bool ShowPois
        {
            get => _showPois;
            set
            {
                if (SetProperty(ref _showPois, value))
                {
                    OnShowPoisChanged?.Invoke();
                }
            }
        }

        public bool IsLoadFromDeviceEnabled => SelectedDevice != null && SelectedFile != null;

        private string? _selectedFile;
        public string? SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (SetProperty(ref _selectedFile, value))
                {
                    OnPropertyChanged(nameof(IsLoadFromDeviceEnabled));
                }
            }
        }

        #endregion

        #region Commands

        public ICommand OpenMissionCommand { get; private set; } = null!;
        public ICommand ScanDevicesCommand { get; private set; } = null!;
        public ICommand LoadFromDeviceCommand { get; private set; } = null!;
        public ICommand RefreshDeviceFilesCommand { get; private set; } = null!;
        public ICommand FitToMissionCommand { get; private set; } = null!;
        
        // Menu Commands
        public ICommand ExitApplicationCommand { get; private set; } = null!;
        public ICommand AboutCommand { get; private set; } = null!;
        public ICommand ShowHelpCommand { get; private set; } = null!;
        public ICommand ClearMissionCommand { get; private set; } = null!;
        public ICommand ExportMissionCommand { get; private set; } = null!;
        public ICommand ShowDeviceInfoCommand { get; private set; } = null!;
        public ICommand ToggleDeviceMonitoringCommand { get; private set; } = null!;
        public ICommand ShowMapOptionsCommand { get; private set; } = null!;

        #endregion

        #region Events

        public event Action? OnMissionLoaded;
        public event Action? OnShowPoisChanged;
        public event Action? OnFitToMission;

        #endregion

        private void InitializeCommands()
        {
            OpenMissionCommand = new AsyncRelayCommand(OpenMissionAsync);
            ScanDevicesCommand = new RelayCommand(() => ScanDevices());
            LoadFromDeviceCommand = new AsyncRelayCommand(LoadFromDeviceAsync, () => IsLoadFromDeviceEnabled);
            RefreshDeviceFilesCommand = new RelayCommand(() => RefreshDeviceFiles());
            FitToMissionCommand = new RelayCommand(() => OnFitToMission?.Invoke());
            
            // Menu Commands
            ExitApplicationCommand = new RelayCommand(() => ExitApplication());
            AboutCommand = new RelayCommand(() => ShowAbout());
            ShowHelpCommand = new RelayCommand(() => ShowHelp());
            ClearMissionCommand = new RelayCommand(() => ClearMission(), () => Mission.Waypoints.Count > 0 || Mission.Pois.Count > 0);
            ExportMissionCommand = new AsyncRelayCommand(() => ExportMissionAsync(), () => Mission.Waypoints.Count > 0);
            ShowDeviceInfoCommand = new RelayCommand(() => ShowDeviceInfo(), () => SelectedDevice != null);
            ToggleDeviceMonitoringCommand = new RelayCommand(() => ToggleDeviceMonitoring());
            ShowMapOptionsCommand = new RelayCommand(() => ShowMapOptions());
        }

        private void InitializeDjiDetection()
        {
            try
            {
                _djiDetector = new DjiRemoteControlDetector();
                _djiDetector.RemoteControlConnected += OnDjiDeviceConnected;
                _djiDetector.RemoteControlDisconnected += OnDjiDeviceDisconnected;
                _djiDetector.ScanStatusChanged += OnScanStatusChanged;
                _djiDetector.StartMonitoring();

                UpdateStatusBar("Initializing device scanner...", true);
                DeviceStatusText = "Initializing device scanner...";
            }
            catch (Exception ex)
            {
                UpdateStatusBar($"Device scan error: {ex.Message}");
                DeviceStatusText = $"Error initializing device detection: {ex.Message}";
            }
        }

        private void OnScanStatusChanged(string status)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateStatusBar(status, status.Contains("Scanning") || status.Contains("rescanning"));
                DeviceStatusText = status;
            });
        }

        private void OnDjiDeviceConnected(DjiRemoteControlDevice device)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!ConnectedDevices.Any(d => d.DeviceId == device.DeviceId))
                {
                    ConnectedDevices.Add(device);
                }

                DeviceCount = ConnectedDevices.Count;
                DeviceStatusText = $"DJI device connected: {device.Name}";
                UpdateStatusBar($"Device connected: {device.Name}", false, true);

                // Auto-select first device if none selected
                if (SelectedDevice == null)
                {
                    SelectedDevice = device;
                }
            });
        }

        private void OnDjiDeviceDisconnected(DjiRemoteControlDevice device)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var deviceToRemove = ConnectedDevices.FirstOrDefault(d => d.DeviceId == device.DeviceId);
                if (deviceToRemove != null)
                {
                    ConnectedDevices.Remove(deviceToRemove);
                }

                DeviceCount = ConnectedDevices.Count;

                if (SelectedDevice?.DeviceId == device.DeviceId)
                {
                    SelectedDevice = null;
                    DeviceFiles.Clear();
                    IsConnected = false;
                    ConnectionStatusText = "Disconnected";
                }

                DeviceStatusText = $"DJI device disconnected: {device.Name}";
                UpdateStatusBar($"Device disconnected: {device.Name}", false, true);

                if (DeviceCount == 0)
                {
                    UpdateStatusBar("No DJI devices connected");
                }
            });
        }

        private void UpdateStatusBar(string message, bool showProgress = false, bool includeTimestamp = false)
        {
            var displayMessage = includeTimestamp
                ? $"[{DateTime.Now:HH:mm:ss}] {message}"
                : message;

            StatusBarText = displayMessage;
            IsScanningVisible = showProgress;
        }

        private async Task OpenMissionAsync()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "DJI missions (*.kmz;*.zip)|*.kmz;*.zip|All files (*.*)|*.*",
                Title = "Select DJI Mission File"
            };

            if (dlg.ShowDialog() != true) return;

            await LoadMissionFromFileAsync(dlg.FileName);
        }

        private async Task LoadMissionFromFileAsync(string filePath)
        {
            try
            {
                UpdateStatusBar("Loading mission file...");

                string tempDir = IOPath.Combine(IOPath.GetTempPath(), "DjiMission_" + Guid.NewGuid().ToString("N"));
                IODirectory.CreateDirectory(tempDir);

                ZipFile.ExtractToDirectory(filePath, tempDir);

                // Find waylines.wpml file
                string? wpml = IODirectory.GetFiles(tempDir, "waylines.wpml", IOSearchOption.AllDirectories).FirstOrDefault()
                            ?? IODirectory.GetFiles(tempDir, "*.wpml", IOSearchOption.AllDirectories).FirstOrDefault()
                            ?? IODirectory.GetFiles(tempDir, "*.kml", IOSearchOption.AllDirectories).FirstOrDefault();

                if (wpml is null)
                {
                    UpdateStatusBar("Error: No mission file found in archive");
                    MessageBox.Show("No waylines.wpml or KML file found in the archive.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                UpdateStatusBar("Parsing mission data...");

                // Parse the mission
                Mission = MissionParser.ParseMission(wpml);

                MissionInfo = $"Mission loaded: {Mission.Waypoints.Count} waypoints, {Mission.Pois.Count} POIs";
                UpdateStatusBar($"Mission loaded: {Mission.Waypoints.Count} waypoints, {Mission.Pois.Count} POIs");

                OnMissionLoaded?.Invoke();

                // Cleanup temp directory
                try { IODirectory.Delete(tempDir, true); } catch { }
            }
            catch (Exception ex)
            {
                UpdateStatusBar($"Error loading mission: {ex.Message}");
                MessageBox.Show($"Error loading mission: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ScanDevices()
        {
            try
            {
                UpdateStatusBar("Restarting device scan...", true);
                _djiDetector?.Dispose();
                InitializeDjiDetection();
                DeviceStatusText = "Restarting device scan...";
            }
            catch (Exception ex)
            {
                UpdateStatusBar($"Scan error: {ex.Message}");
                DeviceStatusText = $"Error scanning devices: {ex.Message}";
            }
        }

        private async Task LoadFromDeviceAsync()
        {
            if (SelectedDevice == null || SelectedFile == null) return;

            try
            {
                UpdateStatusBar("Downloading file from device...", true);
                DeviceStatusText = "Downloading waypoint file...";

                var tempDir = IOPath.Combine(IOPath.GetTempPath(), "DjiDeviceDownload");
                var localFile = await SelectedDevice.DownloadWaypointFile(SelectedFile, tempDir);

                if (localFile != null)
                {
                    await LoadMissionFromFileAsync(localFile);
                    DeviceStatusText = $"Mission loaded from device: {IOPath.GetFileName(SelectedFile)}";
                    UpdateStatusBar($"Mission loaded from device: {IOPath.GetFileName(SelectedFile)}");
                }
                else
                {
                    UpdateStatusBar("Failed to download file from device");
                    DeviceStatusText = "Failed to download file from device";
                }
            }
            catch (Exception ex)
            {
                UpdateStatusBar($"Download error: {ex.Message}");
                MessageBox.Show($"Error loading mission from device: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                DeviceStatusText = $"Error: {ex.Message}";
            }
        }

        private void RefreshDeviceFiles()
        {
            if (SelectedDevice == null) return;

            try
            {
                UpdateStatusBar("Reading device files...");

                var files = SelectedDevice.GetWaypointFiles();
                DeviceFiles.Clear();

                foreach (var file in files)
                {
                    DeviceFiles.Add(file);
                }

                DeviceStatusText = $"Found {files.Count} waypoint files on device";
                UpdateStatusBar($"Found {files.Count} files on device");
            }
            catch (Exception ex)
            {
                UpdateStatusBar($"Error reading device: {ex.Message}");
                DeviceStatusText = $"Error reading device files: {ex.Message}";
            }
        }

        #region Menu Command Implementations

        private void ExitApplication()
        {
            Application.Current.Shutdown();
        }

        private void ShowAbout()
        {
            var about = $@"DJI Waypoint Manager
Version: 2.0.0
Built with .NET 9 and WPF

A tool for viewing and managing DJI drone mission files (.kmz/.zip) with waypoints and POIs.

Features:
• Load missions from files or DJI devices
• Interactive map visualization
• Real-time device detection
• Mission analysis and export

© 2024 - Open Source Project";

            MessageBox.Show(about, "About DJI Waypoint Manager", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowHelp()
        {
            var help = @"Quick Start Guide:

1. LOAD MISSION:
   • File ? Open Mission (KMZ/ZIP)
   • Or connect DJI device and use Device ? Load from Device

2. VIEW MISSION:
   • Left panel shows mission details, waypoints, and POIs
   • Right panel displays interactive map
   • Use View ? Fit to Mission to center view

3. DEVICE OPERATIONS:
   • Device ? Scan for Devices to find connected controllers
   • Device ? Device Information for connected device details
   • Device ? Refresh Files to update file list

4. EXPORT:
   • File ? Export Mission to save in different format

For more help, visit the project documentation.";

            MessageBox.Show(help, "Help - DJI Waypoint Manager", MessageBoxButton.OK, MessageBoxImage.Question);
        }

        private void ClearMission()
        {
            var result = MessageBox.Show("Are you sure you want to clear the current mission? All unsaved changes will be lost.", 
                                       "Clear Mission", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                Mission = new Mission();
                MissionInfo = "No mission loaded";
                UpdateStatusBar("Mission cleared");
                OnMissionLoaded?.Invoke(); // This will clear the map
            }
        }

        private async Task ExportMissionAsync()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "KMZ files (*.kmz)|*.kmz|JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Export Mission",
                DefaultExt = ".json",
                FileName = "mission_export"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                UpdateStatusBar("Exporting mission...");

                if (dlg.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    // Export as JSON
                    var jsonOptions = new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    };
                    
                    var json = System.Text.Json.JsonSerializer.Serialize(Mission, jsonOptions);
                    await IOFile.WriteAllTextAsync(dlg.FileName, json);
                }
                else
                {
                    // For now, just export as JSON even for KMZ
                    MessageBox.Show("KMZ export not yet implemented. Exporting as JSON instead.", "Export", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    var jsonFile = IOPath.ChangeExtension(dlg.FileName, ".json");
                    var jsonOptions = new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    };
                    
                    var json = System.Text.Json.JsonSerializer.Serialize(Mission, jsonOptions);
                    await IOFile.WriteAllTextAsync(jsonFile, json);
                }

                UpdateStatusBar($"Mission exported to {IOPath.GetFileName(dlg.FileName)}");
                MessageBox.Show($"Mission exported successfully to:\n{dlg.FileName}", "Export Complete", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                UpdateStatusBar($"Export error: {ex.Message}");
                MessageBox.Show($"Error exporting mission: {ex.Message}", "Export Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowDeviceInfo()
        {
            if (SelectedDevice == null) return;

            var info = $@"Device Information:

Name: {SelectedDevice.Name}
Device ID: {SelectedDevice.DeviceId}
Type: {SelectedDevice.GetType().Name}
Connection Status: Connected

Waypoint Files: {DeviceFiles.Count}

Connected at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

Note: Additional device details may be available through the DJI SDK.";

            MessageBox.Show(info, $"Device Info - {SelectedDevice.Name}", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ToggleDeviceMonitoring()
        {
            try
            {
                if (_djiDetector == null)
                {
                    UpdateStatusBar("Starting device monitoring...", true);
                    InitializeDjiDetection();
                }
                else
                {
                    UpdateStatusBar("Stopping device monitoring...");
                    _djiDetector.Dispose();
                    _djiDetector = null;
                    ConnectedDevices.Clear();
                    SelectedDevice = null;
                    DeviceCount = 0;
                    IsConnected = false;
                    ConnectionStatusText = "Disconnected";
                    DeviceStatusText = "Device monitoring stopped";
                    UpdateStatusBar("Device monitoring stopped");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusBar($"Error toggling monitoring: {ex.Message}");
                MessageBox.Show($"Error toggling device monitoring: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowMapOptions()
        {
            var options = $@"Map Display Options:

Current Settings:
• Show POIs: {ShowPois}
• Waypoints: {Mission.Waypoints.Count}
• POIs: {Mission.Pois.Count}

Available Actions:
• Use 'Fit to Mission' to center map on mission
• Toggle 'Show POIs' checkbox to show/hide POIs
• Click waypoints/POIs in data grid to highlight on map

Map Features:
• Interactive zoom and pan
• Waypoint path visualization
• POI markers with labels
• Real-time mission updates";

            MessageBox.Show(options, "Map Options", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        public void Dispose()
        {
            _djiDetector?.Dispose();
        }
    }
}