using DjiWaypointManager.Models;

namespace DjiWaypointManager.Services
{
    public interface IDjiDeviceService
    {
        event Action<DjiRemoteControlDevice>? DeviceConnected;
        event Action<DjiRemoteControlDevice>? DeviceDisconnected;
        event Action<string>? StatusChanged;

        void StartMonitoring();
        void StopMonitoring();
        List<DjiRemoteControlDevice> GetConnectedDevices();
        void Dispose();
    }

    public interface IMissionService
    {
        Task<Mission> LoadMissionFromFileAsync(string filePath);
        Task<Mission> LoadMissionFromDeviceAsync(DjiRemoteControlDevice device, string fileName);
    }
}