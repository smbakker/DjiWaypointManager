# DJI Waypoint Manager

A Windows desktop application for managing and visualizing DJI drone waypoint missions. This WPF application provides an intuitive interface for creating, editing, and managing waypoint missions with integrated map visualization and DJI remote controller detection.

## 🚁 Features

- **Interactive Map Interface**: Visualize waypoints, POIs (Points of Interest), and flight paths using Leaflet maps
- **Waypoint Management**: Create, edit, and organize waypoints with configurable parameters:
  - GPS coordinates (latitude/longitude)
  - Execution height and speed
  - Heading modes and angles
  - Custom actions (photo, video recording)
  - Turn damping distance
- **Points of Interest (POI)**: Define and manage POIs for camera targeting
- **Mission Configuration**: Comprehensive mission settings and parameters
- **DJI Remote Controller Detection**: Automatic detection of connected DJI remote controllers
- **Import/Export**: Load and save mission files in various formats
- **Real-time Visualization**: Live map updates with flight path visualization

## 🛠️ Technical Stack

- **.NET 9.0** with Windows targeting
- **WPF (Windows Presentation Foundation)** for the desktop interface
- **WebView2** for embedded web map functionality
- **Leaflet.js** for interactive mapping
- **Device Detection Libraries**:
  - HidLibrary for USB HID device detection
  - MediaDevices for MTP device access
  - System.Management for Windows device monitoring

## 📋 Requirements

- **Operating System**: Windows 10/11
- **.NET 9.0 Runtime** (Windows)
- **WebView2 Runtime** (usually included with Windows 11, downloadable for Windows 10)
- **DJI Remote Controller** (optional, for device detection features)

## 🚀 Getting Started

### Prerequisites

1. Install [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
2. Ensure WebView2 Runtime is installed
3. Clone this repository

### Building the Application

```bash
# Navigate to the source directory
cd src

# Build the solution
dotnet build DjiWaypointManager.sln

# Run the application
dotnet run --project DjiWaypointManager
```

### Alternative: Visual Studio

1. Open `src/DjiWaypointManager.sln` in Visual Studio 2022
2. Set `DjiWaypointManager` as the startup project
3. Build and run (F5)

## 🗺️ Usage

### Creating a Mission

1. **Launch the Application**: The main window will display an interactive map
2. **Add Waypoints**: Click on the map to place waypoints or use the waypoint management interface
3. **Configure Waypoints**: Set parameters for each waypoint:
   - Altitude and speed
   - Camera actions (photo/video)
   - Heading and orientation
4. **Add POIs**: Define points of interest for camera targeting
5. **Save Mission**: Export your mission to a file for use with DJI applications

### Map Controls

- **Zoom**: Mouse wheel or zoom controls
- **Pan**: Click and drag to move around the map
- **Waypoint Selection**: Click on waypoints to select and edit
- **Flight Path**: Visualized with different line styles for straight/curved segments

### Legend

- 🔵 **Blue circles**: Waypoints
- 🔴 **Orange circles**: Points of Interest (POIs)
- **Blue lines**: Straight flight segments
- **Green lines**: Curved flight segments
- **Orange dotted lines**: POI connections
- 🎥 **Camera icon**: Video recording action
- 📸 **Photo icon**: Photo capture action

## 📁 Project Structure

```
src/
├── DjiWaypointManager.sln          # Solution file
└── DjiWaypointManager/
    ├── Models/                     # Data models
    │   ├── Mission.cs              # Mission container
    │   ├── Waypoint.cs             # Waypoint definition
    │   ├── Poi.cs                  # Point of Interest
    │   ├── ActionDef.cs            # Action definitions
    │   └── MissionConfig.cs        # Mission configuration
    ├── Services/                   # Business logic services
    │   ├── DjiRemoteControlDetector.cs    # Device detection
    │   ├── DjiRemoteControlDevice.cs      # Device abstraction
    │   └── MissionParser.cs               # Mission file parsing
    ├── js/                         # JavaScript for map functionality
    │   └── map-core.js             # Core mapping functions
    ├── map.html                    # HTML map interface
    ├── mission-map.css             # Map styling
    ├── mission-map.js              # Map interaction logic
    ├── MainWindow.xaml             # Main UI layout
    ├── MainWindow.xaml.cs          # Main UI logic
    └── App.xaml                    # Application configuration
```

## 🔧 Configuration

The application supports various configuration options through the `MissionConfig` class:

- Flight parameters (altitude, speed limits)
- Safety settings
- Default waypoint behaviors
- Map preferences

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ⚠️ Disclaimer

This application is designed for mission planning and visualization purposes. Always ensure compliance with local aviation regulations and DJI's operational guidelines when flying drones. The developers are not responsible for any misuse of this software or violations of aviation laws.

## 🐛 Known Issues

- Device detection may require administrator privileges on some Windows configurations
- WebView2 initialization might fail on older Windows 10 versions without latest updates

## 📞 Support

For issues, questions, or contributions:

1. Check the [Issues](../../issues) section
2. Create a new issue with detailed description
3. Include system information and error messages when reporting bugs

## 🔄 Version History

- **v1.0.0**: Initial release with basic waypoint management and map visualization
- **Future**: Enhanced DJI integration, additional export formats, flight simulation

---

*Made with ❤️ for the DJI community*