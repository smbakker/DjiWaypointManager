using System.Management;
using System.Diagnostics;
using MediaDevices;
using HidLibrary;

namespace DjiWaypointManager.Services
{
    public class DjiRemoteControlDetector : IDisposable
    {
        private ManagementEventWatcher? _usbWatcher;
        private readonly List<DjiRemoteControlDevice> _connectedDevices = new();
        
        public event Action<DjiRemoteControlDevice>? RemoteControlConnected;
        public event Action<DjiRemoteControlDevice>? RemoteControlDisconnected;
        public event Action<string>? ScanStatusChanged;

        // Known DJI Remote Controller identifiers
        private readonly Dictionary<string, string> _knownDjiRemotes = new()
        {
            // DJI RC-N1 (Mini 2, Air 2S, Mini 3)
            { "VID_2CA3&PID_001F", "DJI RC-N1" },
            { "VID_2CA3&PID_0020", "DJI RC Pro" },
            { "VID_2CA3&PID_0021", "DJI RC" },
            { "VID_2CA3&PID_0022", "DJI RC 2" },
            // DJI Smart Controller
            { "VID_18D1&PID_4EE7", "DJI Smart Controller" },
            { "VID_18D1&PID_4EE8", "DJI Smart Controller Enterprise" },
            // Generic Android devices (for DJI Fly app)
            { "Android", "Android Device with DJI Fly" }
        };

        public void StartMonitoring()
        {
            try
            {
                ScanStatusChanged?.Invoke("Starting device monitoring...");
                
                // Monitor USB device changes
                var query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");
                _usbWatcher = new ManagementEventWatcher(query);
                _usbWatcher.EventArrived += OnDeviceChanged;
                _usbWatcher.Start();

                Debug.WriteLine("DJI Remote Control monitoring started");
                
                // Scan for already connected devices
                ScanForDjiDevices();
            }
            catch (Exception ex)
            {
                ScanStatusChanged?.Invoke($"Error starting monitoring: {ex.Message}");
                Debug.WriteLine($"Error starting DJI device monitoring: {ex.Message}");
            }
        }

        private void ScanForDjiDevices()
        {
            Task.Run(async () =>
            {
                try
                {
                    ScanStatusChanged?.Invoke("Scanning for MTP devices...");
                    
                    // Scan for MTP devices (Android phones/DJI Smart Controllers)
                    await Task.Run(ScanMtpDevices);
                    
                    ScanStatusChanged?.Invoke("Scanning for HID controllers...");
                    
                    // Scan for HID devices (DJI RC controllers)
                    await Task.Run(ScanHidDevices);
                    
                    var deviceCount = _connectedDevices.Count;
                    if (deviceCount > 0)
                    {
                        ScanStatusChanged?.Invoke($"Scan complete: {deviceCount} device(s) found");
                    }
                    else
                    {
                        ScanStatusChanged?.Invoke("Scan complete: No DJI devices found");
                    }
                }
                catch (Exception ex)
                {
                    ScanStatusChanged?.Invoke($"Scan error: {ex.Message}");
                    Debug.WriteLine($"Error in ScanForDjiDevices: {ex.Message}");
                }
            });
        }

        private void ScanMtpDevices()
        {
            try
            {
                var devices = MediaDevice.GetDevices();
                Debug.WriteLine($"Found {devices.Count()} MTP devices to check");
                
                foreach (var device in devices)
                {
                    try
                    {
                        if (IsDjiDevice(device))
                        {
                            var djiDevice = new DjiRemoteControlDevice(device);
                            if (!_connectedDevices.Any(d => d.DeviceId == djiDevice.DeviceId))
                            {
                                _connectedDevices.Add(djiDevice);
                                RemoteControlConnected?.Invoke(djiDevice);
                                Debug.WriteLine($"DJI device connected: {djiDevice.Name}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error checking MTP device: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error scanning MTP devices: {ex.Message}");
            }
        }

        private void ScanHidDevices()
        {
            try
            {
                var hidDevices = HidDevices.Enumerate();
                Debug.WriteLine($"Found {hidDevices.Count()} HID devices to check");
                
                foreach (var device in hidDevices)
                {
                    if (IsDjiHidDevice(device))
                    {
                        var djiDevice = new DjiRemoteControlDevice(device);
                        if (!_connectedDevices.Any(d => d.DeviceId == djiDevice.DeviceId))
                        {
                            _connectedDevices.Add(djiDevice);
                            RemoteControlConnected?.Invoke(djiDevice);
                            Debug.WriteLine($"DJI HID device connected: {djiDevice.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error scanning HID devices: {ex.Message}");
            }
        }

        private bool IsDjiDevice(MediaDevice device)
        {
            var deviceName = device.FriendlyName?.ToLower() ?? "";
            var manufacturer = device.Manufacturer?.ToLower() ?? "";
            
            return deviceName.Contains("dji") || 
                   manufacturer.Contains("dji") ||
                   deviceName.Contains("smart controller") ||
                   (deviceName.Contains("android") && HasDjiFlyApp(device));
        }

        private bool IsDjiHidDevice(HidDevice device)
        {
            // Check VID/PID combinations
            var vidPid = $"VID_{device.Attributes.VendorId:X4}&PID_{device.Attributes.ProductId:X4}";
            return _knownDjiRemotes.ContainsKey(vidPid);
        }

        private bool HasDjiFlyApp(MediaDevice device)
        {
            try
            {
                device.Connect();
                
                // Check for DJI Fly app directories
                var root = device.GetRootDirectory();
                var androidDir = root.EnumerateDirectories()
                    .FirstOrDefault(d => string.Equals(d.Name, "Android", StringComparison.OrdinalIgnoreCase));
                
                if (androidDir != null)
                {
                    var dataDir = androidDir.EnumerateDirectories()
                        .FirstOrDefault(d => string.Equals(d.Name, "data", StringComparison.OrdinalIgnoreCase));
                    
                    if (dataDir != null)
                    {
                        var djiFolders = dataDir.EnumerateDirectories()
                            .Where(d => d.Name.StartsWith("dji.") || d.Name.Contains("dji"))
                            .ToList();
                        
                        device.Disconnect();
                        return djiFolders.Any();
                    }
                }
                
                device.Disconnect();
                return false;
            }
            catch
            {
                try { device.Disconnect(); } catch { }
                return false;
            }
        }

        private void OnDeviceChanged(object sender, EventArrivedEventArgs e)
        {
            Task.Run(() =>
            {
                ScanStatusChanged?.Invoke("Device change detected, rescanning...");
                
                // Re-scan for devices
                ScanForDjiDevices();
                
                // Check for disconnected devices
                var disconnectedDevices = _connectedDevices.Where(d => !d.IsConnected()).ToList();
                foreach (var device in disconnectedDevices)
                {
                    _connectedDevices.Remove(device);
                    RemoteControlDisconnected?.Invoke(device);
                    Debug.WriteLine($"DJI device disconnected: {device.Name}");
                }
            });
        }

        public List<DjiRemoteControlDevice> GetConnectedDevices()
        {
            return _connectedDevices.ToList();
        }

        public void Dispose()
        {
            ScanStatusChanged?.Invoke("Stopping device monitoring...");
            _usbWatcher?.Stop();
            _usbWatcher?.Dispose();
            _connectedDevices.Clear();
        }
    }
}