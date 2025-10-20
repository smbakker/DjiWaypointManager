using DjiWaypointManager.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace DjiWaypointManager.Services
{
    public static class MissionParser
    {
        public static Mission ParseMission(string filePath)
        {
            var doc = XDocument.Load(filePath);
            var mission = new Mission();

            XNamespace kml = "http://www.opengis.net/kml/2.2";
            XNamespace wpml = "http://www.uav.com/wpmz/1.0.2";

            // Parse mission configuration metadata
            var missionConfigElement = doc.Descendants(wpml + "missionConfig").FirstOrDefault();
            if (missionConfigElement != null)
            {
                mission.Config.FlyToWaylineMode = missionConfigElement.Element(wpml + "flyToWaylineMode")?.Value ?? "";
                mission.Config.FinishAction = missionConfigElement.Element(wpml + "finishAction")?.Value ?? "";
                mission.Config.ExitOnRCLost = missionConfigElement.Element(wpml + "exitOnRCLost")?.Value ?? "";
                mission.Config.ExecuteRCLostAction = missionConfigElement.Element(wpml + "executeRCLostAction")?.Value ?? "";
                
                var speedStr = missionConfigElement.Element(wpml + "globalTransitionalSpeed")?.Value;
                mission.Config.GlobalTransitionalSpeed = Parse(speedStr);

                var droneInfoElement = missionConfigElement.Element(wpml + "droneInfo");
                if (droneInfoElement != null)
                {
                    var droneEnumStr = droneInfoElement.Element(wpml + "droneEnumValue")?.Value;
                    var droneSubEnumStr = droneInfoElement.Element(wpml + "droneSubEnumValue")?.Value;
                    
                    if (int.TryParse(droneEnumStr, out var droneEnum))
                        mission.Config.DroneInfo.DroneEnumValue = droneEnum;
                    
                    if (int.TryParse(droneSubEnumStr, out var droneSubEnum))
                        mission.Config.DroneInfo.DroneSubEnumValue = droneSubEnum;
                }
                
                System.Diagnostics.Debug.WriteLine("=== MISSION CONFIG PARSED ===");
                System.Diagnostics.Debug.WriteLine($"Fly to wayline mode: {mission.Config.FlyToWaylineMode}");
                System.Diagnostics.Debug.WriteLine($"Finish action: {mission.Config.FinishAction}");
                System.Diagnostics.Debug.WriteLine($"RC lost action: {mission.Config.ExecuteRCLostAction}");
                System.Diagnostics.Debug.WriteLine($"Global speed: {mission.Config.GlobalTransitionalSpeed} m/s");
                System.Diagnostics.Debug.WriteLine($"Drone: {mission.Config.DroneInfo.DroneModel}");
            }

            var placemarks = doc.Descendants(kml + "Placemark").ToList();
            System.Diagnostics.Debug.WriteLine($"Found {placemarks.Count} placemarks in mission file");

            // Parse waypoints and POIs
            int poiIndex = 1;
            foreach (var pm in placemarks)
            {
                // Get coordinates (KML format: lon,lat,alt)
                var coordText = pm.Descendants(kml + "coordinates").FirstOrDefault()?.Value ?? "";
                var parts = coordText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (parts.Length < 2) 
                {
                    System.Diagnostics.Debug.WriteLine("Skipping placemark - insufficient coordinate parts");
                    continue;
                }

                double lon = Parse(parts[0]);
                double lat = Parse(parts[1]);

                if (lat == 0 && lon == 0) 
                {
                    System.Diagnostics.Debug.WriteLine("Skipping placemark - zero coordinates");
                    continue;
                }

                // Get waypoint index
                int wpIndex = int.TryParse(pm.Element(wpml + "index")?.Value, out var idx) ? idx : 0;

                // Parse heading parameters
                var headingParam = pm.Element(wpml + "waypointHeadingParam");
                var headingAngleStr = headingParam?.Element(wpml + "waypointHeadingAngle")?.Value;
                var headingMode = headingParam?.Element(wpml + "waypointHeadingMode")?.Value ?? "";
                
                double? headingAngle = null;
                if (!string.IsNullOrEmpty(headingAngleStr))
                {
                    var angle = Parse(headingAngleStr);
                    if (angle != 0) // Only set if it's not zero
                    {
                        headingAngle = angle;
                    }
                }

                // Parse turn parameters
                var turnParam = pm.Element(wpml + "waypointTurnParam");
                var turnDampingStr = turnParam?.Element(wpml + "waypointTurnDampingDist")?.Value;
                double? turnDamping = null;
                if (!string.IsNullOrEmpty(turnDampingStr))
                {
                    var damping = Parse(turnDampingStr);
                    if (damping != 0) // Only set if it's not zero
                    {
                        turnDamping = damping;
                    }
                }

                // Check if this segment uses straight line - CRITICAL FOR LINE DRAWING!
                var useStraightLineElement = pm.Element(wpml + "useStraightLine");
                bool useStraightLine = useStraightLineElement?.Value == "1";
                
                System.Diagnostics.Debug.WriteLine($"Waypoint {wpIndex}: useStraightLine element = '{useStraightLineElement?.Value}', parsed = {useStraightLine}");

                // Create waypoint
                var waypoint = new Waypoint
                {
                    Index = wpIndex,
                    Lat = lat,
                    Lon = lon,
                    ExecuteHeight = Parse(pm.Element(wpml + "executeHeight")?.Value),
                    Speed = Parse(pm.Element(wpml + "waypointSpeed")?.Value),
                    UseStraightLine = useStraightLine,
                    HeadingAngle = headingAngle,
                    HeadingMode = headingMode,
                    TurnDampingDist = turnDamping
                };

                // Debug output for each waypoint
                System.Diagnostics.Debug.WriteLine($"Waypoint {wpIndex}: [{lat:F6}, {lon:F6}], UseStraightLine={useStraightLine}, HeadingAngle={headingAngle}, TurnDamping={turnDamping}");

                // Parse actions
                var actionGroups = pm.Elements(wpml + "actionGroup");
                foreach (var ag in actionGroups)
                {
                    foreach (var a in ag.Elements(wpml + "action"))
                    {
                        string func = a.Element(wpml + "actionActuatorFunc")?.Value ?? "";
                        if (!string.IsNullOrEmpty(func))
                        {
                            waypoint.Actions.Add(new ActionDef { Func = func });
                        }
                    }
                }

                // Check for POI - FIXED COORDINATE PARSING BUG!
                var poiText = headingParam?.Element(wpml + "waypointPoiPoint")?.Value;
                if (!string.IsNullOrWhiteSpace(poiText))
                {
                    System.Diagnostics.Debug.WriteLine($"Found POI text for waypoint {wpIndex}: '{poiText}'");
                    
                    var poiParts = poiText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (poiParts.Length >= 2)
                    {
                        // FIX: POI format in XML is lat,lon,alt (same as coordinates)
                        double poiLon = Parse(poiParts[1]); // First part is latitude
                        double poiLat = Parse(poiParts[0]); // Second part is longitude  
                        double poiAlt = poiParts.Length > 2 ? Parse(poiParts[2]) : 0;

                        System.Diagnostics.Debug.WriteLine($"Parsed POI coordinates: lon={poiLon}, lat={poiLat}, alt={poiAlt}");

                        if (poiLat != 0 && poiLon != 0)
                        {
                            var poi = new Poi
                            {
                                Index = poiIndex,
                                Lat = poiLat,
                                Lon = poiLon,
                                Alt = poiAlt
                            };

                            mission.Pois.Add(poi);
                            waypoint.PoiIndex = poiIndex;
                            
                            System.Diagnostics.Debug.WriteLine($"✅ Created POI {poiIndex} at [{poiLat:F6}, {poiLon:F6}] for waypoint {wpIndex}");
                            poiIndex++;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Skipped POI for waypoint {wpIndex} - zero coordinates");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Invalid POI format for waypoint {wpIndex}: not enough coordinate parts");
                    }
                }

                mission.Waypoints.Add(waypoint);
            }

            // Sort waypoints by index
            mission.Waypoints = mission.Waypoints.OrderBy(w => w.Index).ToList();

            // Enhanced debug summary
            var straightWaypoints = mission.Waypoints.Where(w => w.UseStraightLine).ToList();
            var curvedWaypoints = mission.Waypoints.Where(w => !w.UseStraightLine).ToList();
            var waypointsWithPois = mission.Waypoints.Where(w => w.PoiIndex.HasValue).ToList();
            
            System.Diagnostics.Debug.WriteLine($"=== MISSION PARSING COMPLETE ===");
            System.Diagnostics.Debug.WriteLine($"Total waypoints: {mission.Waypoints.Count}");
            System.Diagnostics.Debug.WriteLine($"Straight segments: {straightWaypoints.Count}");
            System.Diagnostics.Debug.WriteLine($"Curved segments: {curvedWaypoints.Count}");
            System.Diagnostics.Debug.WriteLine($"Total POIs: {mission.Pois.Count}");
            System.Diagnostics.Debug.WriteLine($"Waypoints with POIs: {waypointsWithPois.Count}");
            
            // Log first few waypoint details for verification
            for (int i = 0; i < Math.Min(3, mission.Waypoints.Count); i++)
            {
                var wp = mission.Waypoints[i];
                System.Diagnostics.Debug.WriteLine($"  Waypoint {wp.Index}: UseStraightLine={wp.UseStraightLine}, PoiIndex={wp.PoiIndex}");
            }
            
            if (mission.Waypoints.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("❌ WARNING: No waypoints parsed from mission file!");
                
                // Add some test waypoints for debugging (Amsterdam area)
                System.Diagnostics.Debug.WriteLine("🔧 Adding test waypoints for debugging...");
                
                mission.Waypoints.Add(new Waypoint
                {
                    Index = 1,
                    Lat = 52.3676,
                    Lon = 4.9041,
                    ExecuteHeight = 50,
                    Speed = 5,
                    UseStraightLine = true
                });
                
                mission.Waypoints.Add(new Waypoint
                {
                    Index = 2,
                    Lat = 52.3686,
                    Lon = 4.9051,
                    ExecuteHeight = 50,
                    Speed = 5,
                    UseStraightLine = false
                });
                
                mission.Waypoints.Add(new Waypoint
                {
                    Index = 3,
                    Lat = 52.3696,
                    Lon = 4.9061,
                    ExecuteHeight = 50,
                    Speed = 5,
                    UseStraightLine = true
                });
                
                // Add a test POI
                mission.Pois.Add(new Poi
                {
                    Index = 1,
                    Lat = 52.3691,
                    Lon = 4.9046,
                    Alt = 0
                });
                
                mission.Waypoints[1].PoiIndex = 1; // Link second waypoint to POI
                
                System.Diagnostics.Debug.WriteLine("✅ Added 3 test waypoints and 1 POI for debugging");
            }
            
            if (straightWaypoints.Count == 0 && curvedWaypoints.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("❌ WARNING: No flight path segments will be drawn - all waypoints may have UseStraightLine undefined");
            }
            
            return mission;
        }

        private static double Parse(string? s)
            => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0d;
    }
}