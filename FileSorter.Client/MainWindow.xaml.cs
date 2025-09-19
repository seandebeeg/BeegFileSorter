using FileSorter.Shared;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;
using System.IO;
using System.Windows.Threading;
using System.Diagnostics;

namespace FileSorter.Client
{
  public partial class MainWindow : Window
  {
    private readonly SettingsFileStore _settingsStore = new();
    private readonly DispatcherTimer _statusTimer;

    public MainWindow()
    {
      InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      LoadSettings();
    }

    private void LoadSettings()
    {
      if (File.Exists(_settingsStore.ConfigPath))
      {
        var settings = _settingsStore.Load();
        WatchPathBox.Text = settings.WatchPath;
        DestPathBox.Text = settings.DestinationPath;
      }
    }

    private void BrowseWatch(object sender, RoutedEventArgs e)
    {
      if (ShowFolderDialog(out string? path))
        WatchPathBox.Text = path;
    }

    private void BrowseDest(object sender, RoutedEventArgs e)
    {
      if (ShowFolderDialog(out string? path))
        DestPathBox.Text = path;
    }

    private bool ShowFolderDialog(out string? selectedPath)
    {
      var dialog = new CommonOpenFileDialog
      {
        IsFolderPicker = true
      };

      if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
      {
        selectedPath = dialog.FileName;
        return true;
      }

      selectedPath = null;
      return false;
    }

    private async void SaveSettings(object sender, RoutedEventArgs e)
    {
      try
      {
        var settings = new FileSorter.Shared.FileSortSettings
        {
          WatchPath = WatchPathBox.Text,
          DestinationPath = DestPathBox.Text
        };

        _settingsStore.Save(settings);
        StatusText.Text = "Settings saved successfully";

        await Task.Delay(2000);
        StatusText.Text = string.Empty;
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  }
}
