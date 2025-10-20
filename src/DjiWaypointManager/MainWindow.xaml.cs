using DjiWaypointManager.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace DjiWaypointManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            
            // Set up keyboard shortcuts
            SetupKeyboardShortcuts();
        }

        private void SetupKeyboardShortcuts()
        {
            // Ctrl+O - Open Mission
            var openBinding = new KeyBinding(_viewModel.OpenMissionCommand, Key.O, ModifierKeys.Control);
            InputBindings.Add(openBinding);

            // Ctrl+E - Export Mission
            var exportBinding = new KeyBinding(_viewModel.ExportMissionCommand, Key.E, ModifierKeys.Control);
            InputBindings.Add(exportBinding);

            // F1 - Help
            var helpBinding = new KeyBinding(_viewModel.ShowHelpCommand, Key.F1, ModifierKeys.None);
            InputBindings.Add(helpBinding);

            // F5 - Scan Devices
            var scanBinding = new KeyBinding(_viewModel.ScanDevicesCommand, Key.F5, ModifierKeys.None);
            InputBindings.Add(scanBinding);

            // Ctrl+R - Refresh Device Files
            var refreshBinding = new KeyBinding(_viewModel.RefreshDeviceFilesCommand, Key.R, ModifierKeys.Control);
            InputBindings.Add(refreshBinding);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel?.Dispose();
        }
    }
}