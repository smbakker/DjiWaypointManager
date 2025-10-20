// map-core.js - Core map initialization and utilities
var map = L.map('map').setView([52.37, 4.89], 13);
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { 
    attribution: '© OpenStreetMap contributors' 
}).addTo(map);

// Global state
var waypointMarkers = [];
var poiMarkers = [];
var pathLines = [];
var allBounds = null;

// Utility functions
function clearMap() { 
    waypointMarkers.forEach(m => map.removeLayer(m)); 
    waypointMarkers = []; 
    poiMarkers.forEach(m => map.removeLayer(m)); 
    poiMarkers = []; 
    pathLines.forEach(p => map.removeLayer(p)); 
    pathLines = []; 
    allBounds = null; 
}

function fitToMission() { 
    if (allBounds) map.fitBounds(allBounds, { padding: [30, 30] }); 
}

// Legacy support functions
function showWaypoints(json) { 
    showMission(json, '[]', true); 
}

function highlight(lat, lon) { 
    map.setView([lat, lon], Math.max(map.getZoom(), 16)); 
}

// Export globals for other modules
window.MapCore = {
    map,
    waypointMarkers,
    poiMarkers,
    pathLines,
    allBounds,
    clearMap,
    fitToMission,
    showWaypoints,
    highlight
};