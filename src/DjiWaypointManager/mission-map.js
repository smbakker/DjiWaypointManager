// mission-map.js
// Mission viewer logic with speed-based true arc smoothing.

var map = L.map('map').setView([52.37, 4.89], 13);

// Define only two tile layers - OpenStreetMap and Satellite
var osmLayer = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { 
    attribution: '© OpenStreetMap contributors',
    name: 'OpenStreetMap'
});

var satelliteLayer = L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
    attribution: '© Esri, Maxar, Earthstar Geographics, and the GIS User Community',
    name: 'Satellite'
});

// Set default layer
osmLayer.addTo(map);

// Create base layer group for layer control with only 2 options
var baseLayers = {
    "OpenStreetMap": osmLayer,
    "Satellite": satelliteLayer
};

// Add layer control to map
var layerControl = L.control.layers(baseLayers, null, {
    position: 'topright',
    collapsed: false
});
layerControl.addTo(map);

var waypointMarkers = [], poiMarkers = [], pathLines = [], allBounds = null;

function iconForAction(actions){ if(!actions||actions.length===0)return""; return actions.map(a=>{var f=a.func||a.Func||a; if(f==="startRecord")return"🎥"; if(f==="stopRecord")return"⏹"; if(f==="takePhoto")return"📸"; return"⚙";}).join(" ")} 
function clearMap(){ waypointMarkers.forEach(m=>map.removeLayer(m)); waypointMarkers=[]; poiMarkers.forEach(m=>map.removeLayer(m)); poiMarkers=[]; pathLines.forEach(p=>map.removeLayer(p)); pathLines=[]; allBounds=null; }

function createWaypointMarkers(waypoints, pois) { // Pass in POIs
    waypoints.forEach((w, idx) => {
        const lat = w.lat ?? w.Lat ?? 0;
        const lon = w.lon ?? w.Lon ?? 0;
        if (lat === 0 && lon === 0) return;
        const index = w.index ?? w.Index ?? idx + 1;
        const exec = w.executeHeight ?? w.ExecuteHeight ?? 0;
        const speed = w.speed ?? w.Speed ?? 0;
        const acts = w.actions ?? w.Actions ?? [];
        const ll = [lat, lon];
        if (!allBounds) allBounds = L.latLngBounds([ll, ll]); else allBounds.extend(ll);
        const marker = L.marker(ll, { icon: L.divIcon({ className: "waypoint-label", html: index.toString(), iconSize: [28, 28], iconAnchor: [14, 14] }) }).addTo(map);
        marker.bindPopup(`<b>Waypoint ${index}</b><br/>Lat: ${lat.toFixed(6)}<br/>Lon: ${lon.toFixed(6)}<br/>Altitude: ${exec} m<br/>Speed: ${speed} m/s<br/>Actions: ${iconForAction(acts) || 'None'}`);
        marker.waypointIndex = index;
        waypointMarkers.push(marker);

        // --- ENHANCED POI CONNECTION LOGIC WITH DEBUGGING ---
        const poiIndex = w.poiIndex ?? w.PoiIndex;
        console.log(`Waypoint ${index}: checking poiIndex = ${poiIndex}, pois available = ${pois ? pois.length : 'none'}`);
        
        if (poiIndex && pois && pois.length > 0) {
            console.log(`Looking for POI with index ${poiIndex} in`, pois.map(p => ({index: p.index ?? p.Index, lat: p.lat ?? p.Lat, lon: p.lon ?? p.Lon})));
            const poi = pois.find(p => (p.index ?? p.Index) === poiIndex);
            if (poi) {
                const poiLat = poi.lat ?? poi.Lat;
                const poiLon = poi.lon ?? poi.Lon;
                console.log(`✅ Found POI ${poiIndex} at [${poiLat}, ${poiLon}] for waypoint ${index}`);
                if (poiLat && poiLon) {
                    const poiLL = [poiLat, poiLon];
                    allBounds.extend(poiLL);
                    const poiLine = L.polyline([ll, poiLL], {
                        color: "orange",
                        dashArray: "8,4",
                        weight: 2,
                        opacity: 0.7
                    }).addTo(map);
                    pathLines.push(poiLine);
                    console.log(`🔗 Drew orange line from waypoint ${index} to POI ${poiIndex}`);
                } else {
                    console.log(`❌ POI ${poiIndex} has invalid coordinates: [${poiLat}, ${poiLon}]`);
                }
            } else {
                console.log(`❌ POI with index ${poiIndex} not found for waypoint ${index}`);
            }
        } else if (poiIndex) {
            console.log(`❌ Waypoint ${index} has poiIndex ${poiIndex} but no POIs available`);
        }
        // --- END OF ENHANCED LOGIC ---
    });
}
function createPoiMarkers(pois,show){ if(!show)return; pois.forEach(p=>{const lat=p.lat??p.Lat??0, lon=p.lon??p.Lon??0; if(lat===0&&lon===0)return; const idx=p.index??p.Index??0; const alt=p.alt??p.Alt??0; const ll=[lat,lon]; if(!allBounds) allBounds=L.latLngBounds([ll,ll]); else allBounds.extend(ll); const marker=L.marker(ll,{icon:L.divIcon({className:"poi-marker",html:"P",iconSize:[24,24],iconAnchor:[12,12]})}).addTo(map); marker.bindPopup(`<b>POI ${idx}</b><br/>Lat: ${lat.toFixed(6)}<br/>Lon: ${lon.toFixed(6)}<br/>Altitude: ${alt} m`); marker.poiIndex=idx; poiMarkers.push(marker);}); }

// Geometry helpers
const R_EARTH = 6371000; // meters
const DEG2RAD = Math.PI/180; const RAD2DEG = 180/Math.PI;
function _toLocal(baseLat, baseLon, lat, lon){ const latRad=baseLat*DEG2RAD; const dy=(lat-baseLat)*DEG2RAD*R_EARTH; const dx=(lon-baseLon)*DEG2RAD*Math.cos(latRad)*R_EARTH; return [dx,dy]; }
function _fromLocal(baseLat, baseLon, x, y){ const latRad=baseLat*DEG2RAD; const dLat=y/R_EARTH*RAD2DEG; const dLon=x/(R_EARTH*Math.cos(latRad))*RAD2DEG; return [baseLat+dLat, baseLon+dLon]; }
function _norm(v){ const m=Math.hypot(v[0],v[1]); return m===0?[0,0]:[v[0]/m,v[1]/m]; }
function _angle(u,v){ let d=u[0]*v[0]+u[1]*v[1]; d=Math.min(1,Math.max(-1,d)); return Math.acos(d); }
function _angleDeg(p_minus_1, p_plus_1, p_plus_2) {
    const bLat = p_plus_1.lat ?? p_plus_1.Lat, bLon = p_plus_1.lon ?? p_plus_1.Lon;
    const v_in = _toLocal(bLat, bLon, p_minus_1.lat ?? p_minus_1.Lat, p_minus_1.lon ?? p_minus_1.Lon);
    const v_out = _toLocal(bLat, bLon, p_plus_2.lat ?? p_plus_2.Lat, p_plus_2.lon ?? p_plus_2.Lon);
    const u_in = _norm([-v_in[0], -v_in[1]]);
    const u_out = _norm(v_out);
    return _angle(u_in, u_out) * RAD2DEG;
}


function createFlightPath(waypoints) {
    if (!Array.isArray(waypoints) || waypoints.length < 2) return;

    console.log('🛤️ Creating flight path for', waypoints.length, 'waypoints');

    // 1. Generate the full, smooth spline for the entire path
    const allPoints = waypoints.map(wp => [wp.lon ?? wp.Lon, wp.lat ?? wp.Lat]);
    const fullLine = turf.lineString(allPoints);
    const fullSpline = turf.bezierSpline(fullLine, { sharpness: 0.85 });
    const splineCoords = fullSpline.geometry.coordinates.map(p => [p[1], p[0]]); // back to [lat, lon]

    // 2. Iterate through waypoint pairs and decide whether to draw a straight line or a piece of the spline
    for (let i = 0; i < waypoints.length - 1; i++) {
        const p0 = waypoints[i];
        const p1 = waypoints[i + 1];

        const p0_lat = p0.lat ?? p0.Lat, p0_lon = p0.lon ?? p0.Lon;
        const p1_lat = p1.lat ?? p1.Lat, p1_lon = p1.lon ?? p1.Lon;

        // A segment is straight if the *destination* waypoint is marked as straight.
        const isStraight = (p1.useStraightLine === true || p1.UseStraightLine === true);

        // Enhanced debugging for UseStraightLine property
        console.log(`Segment ${i} -> ${i+1}:`);
        console.log(`  From waypoint ${p0.index ?? p0.Index}: useStraightLine=${p0.useStraightLine}, UseStraightLine=${p0.UseStraightLine}`);
        console.log(`  To waypoint ${p1.index ?? p1.Index}: useStraightLine=${p1.useStraightLine}, UseStraightLine=${p1.UseStraightLine}`);
        console.log(`  Segment decision: ${isStraight ? 'STRAIGHT' : 'CURVED'}`);

        if (isStraight) {
            console.log(`  ➡️ Drawing straight line from [${p0_lat}, ${p0_lon}] to [${p1_lat}, ${p1_lon}]`);
            addStraight(p0_lat, p0_lon, p1_lat, p1_lon);
        } else {
            console.log(`  🌀 Drawing curved path from [${p0_lat}, ${p0_lon}] to [${p1_lat}, ${p1_lon}]`);
            // Find the portion of the spline between p0 and p1
            const segmentSpline = [];
            let foundP0 = false;
            for (const coord of splineCoords) {
                if (!foundP0) {
                    // Find the spline point closest to p0
                    if (turf.distance(turf.point([p0_lon, p0_lat]), turf.point([coord[1], coord[0]])) < 0.01) {
                         foundP0 = true;
                    }
                }
                
                if (foundP0) {
                    segmentSpline.push(coord);
                    // Stop when we find the point closest to p1
                    if (turf.distance(turf.point([p1_lon, p1_lat]), turf.point([coord[1], coord[0]])) < 0.01) {
                        break;
                    }
                }
            }
            
            if (segmentSpline.length > 1) {
                const path = L.polyline(segmentSpline, { color: 'green', weight: 4, opacity: 0.9 });
                try { path.arrowheads && path.arrowheads({ size: '12px', frequency: '100px' }); } catch {}
                path.addTo(map);
                pathLines.push(path);
                console.log(`  ✅ Drew curved segment with ${segmentSpline.length} points`);
            } else {
                // Fallback to straight if spline segment not found
                console.log(`  ⚠️ Fallback to straight - spline segment not found`);
                addStraight(p0_lat, p0_lon, p1_lat, p1_lon);
            }
        }
    }
}

function addStraight(lat1, lon1, lat2, lon2) {
    if (!lat1 || !lon1 || !lat2 || !lon2 || (lat1 === lat2 && lon1 === lon2)) return;
    const seg = L.polyline([[lat1, lon1], [lat2, lon2]], { color: 'blue', weight: 4, opacity: 0.8 });
    try { seg.arrowheads && seg.arrowheads({ size: '12px', frequency: '120px' }); } catch {}
    seg.addTo(map);
    pathLines.push(seg);
}


function showMission(waypointsJson, poisJson, showPois) {
    try {
        const waypoints = JSON.parse(waypointsJson);
        const pois = JSON.parse(poisJson);
        clearMap();
        if (!Array.isArray(waypoints) || waypoints.length === 0) return;
        createWaypointMarkers(waypoints, pois); // Pass pois
        createFlightPath(waypoints);
        createPoiMarkers(pois, showPois);
        if (allBounds) map.fitBounds(allBounds, { padding: [20, 20] });
    } catch (e) {
        console.error('showMission error', e);
    }
}
function highlightWaypoint(lat,lon,index){ waypointMarkers.forEach(m=>m.getElement()?.classList.remove('waypoint-selected')); const m=waypointMarkers.find(x=>x.waypointIndex===index); if(m){ m.getElement()?.classList.add('waypoint-selected'); map.setView([lat,lon],Math.max(map.getZoom(),16)); m.openPopup(); }}
function highlightPoi(lat,lon,index){ poiMarkers.forEach(m=>m.getElement()?.classList.remove('poi-selected')); const m=poiMarkers.find(x=>x.poiIndex===index); if(m){ m.getElement()?.classList.add('poi-selected'); map.setView([lat,lon],Math.max(map.getZoom(),16)); m.openPopup(); }}
function fitToMission(){ if(allBounds) map.fitBounds(allBounds,{padding:[30,30]}); }
function showWaypoints(json){ showMission(json,'[]',true); }
function highlight(lat,lon){ map.setView([lat,lon],Math.max(map.getZoom(),16)); }
function createTestMission() {
    const testWaypoints = [
        { index: 1, lat: 52.3676, lon: 4.9041, speed: 4, useStraightLine: false },
        { index: 2, lat: 52.3686, lon: 4.9051, speed: 7, useStraightLine: true },
        { index: 3, lat: 52.3696, lon: 4.9066, speed: 10, useStraightLine: false, poiIndex: 1 },
        { index: 4, lat: 52.3702, lon: 4.9072, speed: 8, useStraightLine: false },
        { index: 5, lat: 52.3711, lon: 4.9060, speed: 6, useStraightLine: false },
        { index: 6, lat: 52.3715, lon: 4.9045, speed: 4, useStraightLine: true }]; const testPois = [{ index: 1, lat: 52.3691, lon: 4.9046, alt: 0 }]; console.log('🧪 Test mission created with mixed straight/curved segments: WP1->2(straight), WP2->3(curved), WP3->4(curved), WP4->5(straight), WP5->6(curved)'); showMission(JSON.stringify(testWaypoints), JSON.stringify(testPois), true);
} 

// Add functions to switch map layers programmatically
function switchToSatellite() {
    // Remove all other layers but keep markers and paths
    map.eachLayer(function (layer) {
        if (layer._url) { // Only remove tile layers
            map.removeLayer(layer);
        }
    });
    if (!map.hasLayer(satelliteLayer)) {
        map.addLayer(satelliteLayer);
    }
}

function switchToOpenStreetMap() {
    map.eachLayer(function (layer) {
        if (layer._url) {
            map.removeLayer(layer);
        }
    });
    if (!map.hasLayer(osmLayer)) {
        map.addLayer(osmLayer);
    }
}

// Expose layer switching functions
window.switchToSatellite = switchToSatellite;
window.switchToOpenStreetMap = switchToOpenStreetMap;

window.showMission=showMission; window.showWaypoints=showWaypoints; window.highlightWaypoint=highlightWaypoint; window.highlightPoi=highlightPoi; window.fitToMission=fitToMission; window.highlight=highlight; window.createTestMission=createTestMission;
setTimeout(()=>{ if(waypointMarkers.length===0) createTestMission(); },2000);
