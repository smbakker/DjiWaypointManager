using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjiWaypointManager.Models
{
    public class Mission
    {
        public List<Waypoint> Waypoints { get; set; } = new();
        public List<Poi> Pois { get; set; } = new();
        public MissionConfig Config { get; set; } = new();
    }
}
