using Microsoft.Web.WebView2.Wpf;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using DjiWaypointManager.ViewModels;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;

namespace DjiWaypointManager.UserControls
{
    /// <summary>
    /// Interaction logic for MapControl.xaml
    /// </summary>
    public partial class MapControl : UserControl
    {
        private WebView2? _webView;
        private bool _isWebViewReady = false;
        private MainViewModel? _viewModel;

        public MapControl()
        {
            InitializeComponent();
            Loaded += MapControl_Loaded;
            DataContextChanged += MapControl_DataContextChanged;
        }

        private void MapControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.OnMissionLoaded -= OnMissionLoaded;
                _viewModel.OnShowPoisChanged -= OnShowPoisChanged;
                _viewModel.OnFitToMission -= OnFitToMission;
            }

            _viewModel = DataContext as MainViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.OnMissionLoaded += OnMissionLoaded;
                _viewModel.OnShowPoisChanged += OnShowPoisChanged;
                _viewModel.OnFitToMission += OnFitToMission;
            }
        }

        private async void MapControl_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebViewAsync();
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                // Create WebView2 control programmatically
                _webView = new WebView2();
                WebViewContainer.Child = _webView;

                await _webView.EnsureCoreWebView2Async();

                // Get the path to the HTML file
                var htmlPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "map.html");

                if (!IOFile.Exists(htmlPath))
                {
                    MessageBox.Show($"Error initializing WebView2: Could not load map.html", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Add navigation completed handler
                _webView.NavigationCompleted += WebView_NavigationCompleted;

                _webView.Source = new Uri(htmlPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing map: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WebView_NavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                _isWebViewReady = true;
                System.Diagnostics.Debug.WriteLine("WebView navigation completed successfully");

                // If we already have a mission loaded, render it now
                if (_viewModel?.Mission != null && (_viewModel.Mission.Waypoints.Any() || _viewModel.Mission.Pois.Any()))
                {
                    _ = RenderMissionOnMapAsync();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"WebView navigation failed: {e.WebErrorStatus}");
            }
        }

        private async void OnMissionLoaded()
        {
            if (_isWebViewReady)
            {
                await RenderMissionOnMapAsync();
            }
        }

        private async void OnShowPoisChanged()
        {
            if (_isWebViewReady)
            {
                await RenderMissionOnMapAsync();
            }
        }

        private async void OnFitToMission()
        {
            if (_webView?.CoreWebView2 != null && _isWebViewReady)
            {
                try
                {
                    var script = "try { window.fitToMission(); } catch(e) { console.error('Error fitting to mission:', e); }";
                    await _webView.CoreWebView2.ExecuteScriptAsync(script);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error fitting map to mission: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async Task RenderMissionOnMapAsync()
        {
            if (_webView?.CoreWebView2 == null || !_isWebViewReady || _viewModel?.Mission == null)
            {
                System.Diagnostics.Debug.WriteLine("WebView not ready for rendering mission");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"Rendering mission: {_viewModel.Mission.Waypoints.Count} waypoints, {_viewModel.Mission.Pois.Count} POIs");

                // Create waypoint data for JavaScript (including POI references)
                var waypointsForJs = _viewModel.Mission.Waypoints.Select(w => new
                {
                    w.Index,
                    w.Lat,
                    w.Lon,
                    w.ExecuteHeight,
                    w.Speed,
                    w.UseStraightLine,
                    w.TurnDampingDist,
                    Actions = w.Actions.Select(a => new { a.Func }).ToList(),
                    // Include POI data if this waypoint references a POI
                    poiIndex = w.PoiIndex.HasValue
                        ? _viewModel.Mission.Pois.Where(p => p.Index == w.PoiIndex.Value)
                                      .Select(p => p.Index)
                                      .FirstOrDefault()
                        : -1
                }).ToList();

                // Create standalone POI data for JavaScript
                var poisForJs = _viewModel.Mission.Pois.Select(p => new
                {
                    p.Index,
                    p.Lat,
                    p.Lon,
                    p.Alt
                }).ToList();

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };

                var waypointsJson = JsonSerializer.Serialize(waypointsForJs, options);
                var poisJson = JsonSerializer.Serialize(poisForJs, options);
                var showPois = _viewModel.ShowPois;

                // Enhanced JavaScript to handle both waypoints and standalone POIs
                var script = $@"
                try {{
                    console.log('=== Rendering Mission ===');
                    var waypointsData = {waypointsJson};
                    var poisData = {poisJson};
                    var showPois = {showPois.ToString().ToLower()};
                    
                    console.log('Waypoints count:', waypointsData.length);
                    console.log('POIs count:', poisData.length);
                    
                    window.showMission(JSON.stringify(waypointsData), JSON.stringify(poisData), showPois);
                    console.log('=== Mission Rendering Complete ===');
                }} catch(e) {{
                    console.error('❌ Error rendering mission:', e);
                    console.error('Stack trace:', e.stack);
                }}";

                await _webView.CoreWebView2.ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine("Mission render script executed");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error rendering mission on map: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                System.Diagnostics.Debug.WriteLine($"RenderMissionOnMapAsync error: {ex}");
            }
        }
    }
}