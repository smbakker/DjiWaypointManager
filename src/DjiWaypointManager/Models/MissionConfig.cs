using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjiWaypointManager.Models
{
    public class MissionConfig
    {
        public string FlyToWaylineMode { get; set; } = "";
        public string FinishAction { get; set; } = "";
        public string ExitOnRCLost { get; set; } = "";
        public string ExecuteRCLostAction { get; set; } = "";
        public double GlobalTransitionalSpeed { get; set; }
        public DroneInfo DroneInfo { get; set; } = new();
    }

    public class DroneInfo
    {
        public int DroneEnumValue { get; set; }
        public int DroneSubEnumValue { get; set; }
        
        public string DroneModel => GetDroneModelName(DroneEnumValue, DroneSubEnumValue);
        
        private static string GetDroneModelName(int enumValue, int subEnumValue)
        {
            return enumValue switch
            {
                60 => "DJI Mini 2",
                61 => "DJI Air 2S",
                67 => "DJI Mini 3",
                68 => "DJI Mini 3 Pro",
                69 => "DJI Air 3",
                70 => "DJI Mavic 3",
                71 => "DJI Mavic 3 Classic",
                72 => "DJI Mavic 3 Pro",
                73 => "DJI Mini 4 Pro",
                _ => $"Unknown Drone (Code: {enumValue}/{subEnumValue})"
            };
        }
    }
}