using MediaDevices;
using HidLibrary;
using System.Diagnostics;
using System.IO;

namespace DjiWaypointManager.Services
{
    public class DjiRemoteControlDevice
    {
        private MediaDevice? _mtpDevice;
        private HidDevice? _hidDevice;

        public string DeviceId { get; private set; }
        public string Name { get; private set; }
        public DjiDeviceType DeviceType { get; private set; }

        public DjiRemoteControlDevice(MediaDevice mtpDevice)
        {
            _mtpDevice = mtpDevice;
            DeviceId = mtpDevice.DeviceId ?? Guid.NewGuid().ToString();
            Name = mtpDevice.FriendlyName ?? "Unknown DJI Device";
            DeviceType = DjiDeviceType.SmartController;
        }

        public DjiRemoteControlDevice(HidDevice hidDevice)
        {
            _hidDevice = hidDevice;
            DeviceId = hidDevice.DevicePath;
            Name = $"DJI RC Controller (VID:{hidDevice.Attributes.VendorId:X4}, PID:{hidDevice.Attributes.ProductId:X4})";
            DeviceType = DjiDeviceType.RCController;
        }

        public bool IsConnected()
        {
            if (_mtpDevice != null)
            {
                try
                {
                    return _mtpDevice.IsConnected;
                }
                catch
                {
                    return false;
                }
            }

            if (_hidDevice != null)
            {
                return _hidDevice.IsConnected;
            }

            return false;
        }

        public List<string> GetWaypointFiles()
        {
            var waypointFiles = new List<string>();

            if (_mtpDevice != null && DeviceType == DjiDeviceType.SmartController)
            {
                waypointFiles.AddRange(GetWaypointFilesFromMtp());
            }

            return waypointFiles;
        }

        private List<string> GetWaypointFilesFromMtp()
        {
            var files = new List<string>();

            try
            {
                _mtpDevice?.Connect();

                // DJI Fly app paths for waypoint files
                var searchPaths = new[]
                {
                    "Android/data/dji.go.v5/files/waylines/",
                    "Android/data/dji.pilot2/files/waylines/",
                    "Android/data/dji.fly/files/waylines/",
                    "Internal shared storage/Android/data/dji.go.v5/files/waylines/",
                    "DJI/waylines/",
                    "DCIM/DJI/waylines/"
                };

                foreach (var searchPath in searchPaths)
                {
                    try
                    {
                        var directory = GetDirectoryFromPath(searchPath);
                        if (directory != null)
                        {
                            var waypointFiles = directory.EnumerateFiles("*.kmz")
                                .Concat(directory.EnumerateFiles("*.wpml"))
                                .Concat(directory.EnumerateFiles("*.zip"))
                                .ToList();

                            foreach (var file in waypointFiles)
                            {
                                files.Add(file.FullName);
                                Debug.WriteLine($"Found waypoint file: {file.FullName}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error searching path {searchPath}: {ex.Message}");
                    }
                }

                _mtpDevice?.Disconnect();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting waypoint files: {ex.Message}");
                try { _mtpDevice?.Disconnect(); } catch { }
            }

            return files;
        }

        private MediaDirectoryInfo? GetDirectoryFromPath(string path)
        {
            if (_mtpDevice == null) return null;

            try
            {
                var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                MediaDirectoryInfo? currentDir = _mtpDevice.GetRootDirectory();

                foreach (var part in parts)
                {
                    if (currentDir == null) return null;
                    
                    currentDir = currentDir.EnumerateDirectories()
                        .FirstOrDefault(d => string.Equals(d.Name, part, StringComparison.OrdinalIgnoreCase));
                    
                    if (currentDir == null) return null;
                }

                return currentDir;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating to path {path}: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> DownloadWaypointFile(string remotePath, string localDirectory)
        {
            if (_mtpDevice == null) return null;

            try
            {
                _mtpDevice.Connect();

                var fileName = Path.GetFileName(remotePath);
                var localPath = Path.Combine(localDirectory, fileName);

                // Ensure local directory exists
                Directory.CreateDirectory(localDirectory);

                // Find the file on the device
                var file = FindFileFromPath(remotePath);
                if (file != null)
                {
                    using var stream = file.OpenRead();
                    using var fileStream = File.Create(localPath);
                    await stream.CopyToAsync(fileStream);
                    
                    Debug.WriteLine($"Downloaded: {remotePath} -> {localPath}");
                    return localPath;
                }

                _mtpDevice.Disconnect();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error downloading file {remotePath}: {ex.Message}");
                try { _mtpDevice?.Disconnect(); } catch { }
            }

            return null;
        }

        private MediaFileInfo? FindFileFromPath(string filePath)
        {
            if (_mtpDevice == null) return null;

            try
            {
                var directory = Path.GetDirectoryName(filePath)?.Replace('\\', '/') ?? "";
                var fileName = Path.GetFileName(filePath);

                var dir = GetDirectoryFromPath(directory);
                return dir?.EnumerateFiles().FirstOrDefault(f => f.Name == fileName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error finding file {filePath}: {ex.Message}");
                return null;
            }
        }
    }

    public enum DjiDeviceType
    {
        RCController,
        SmartController,
        AndroidDevice
    }
}