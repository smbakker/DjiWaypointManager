using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjiWaypointManager.Models
{
    public class Waypoint
    {
        public int Index { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double ExecuteHeight { get; set; }
        public double Speed { get; set; }
        public bool UseStraightLine { get; set; }
        public int? PoiIndex { get; set; }  // reference to global POI list
        public double? HeadingAngle { get; set; }
        public string HeadingMode { get; set; } = "";
        public List<ActionDef> Actions { get; set; } = new();
        public double? TurnDampingDist { get; set; }

        // Property for XAML binding
        public string ActionsText => Actions.Count == 0
            ? ""
            : string.Join(", ", Actions.Select(a => a.Func));
    }
}
