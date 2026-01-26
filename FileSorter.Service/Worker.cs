using FileSorter.Shared;
using Microsoft.Extensions.Options;

namespace FileSorter;

public class Worker : BackgroundService
{
  private readonly ILogger<Worker> _logger;
  private readonly IOptionsMonitor<FileSortSettings> _settingsMonitor;
  private readonly object _sync = new();
  private FileSystemWatcher? _watcher;
  private FileSortSettings _settings;

  public Worker(ILogger<Worker> logger, IOptionsMonitor<FileSortSettings> settingsMonitor)
  {
    _logger = logger;
    _settingsMonitor = settingsMonitor;
    _settings = settingsMonitor.CurrentValue;

    _settingsMonitor.OnChange(updated =>
    {
      lock (_sync)
      {
        bool pathsChanged =
            !string.Equals(_settings.WatchPath, updated.WatchPath, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(_settings.DestinationPath, updated.DestinationPath, StringComparison.OrdinalIgnoreCase);

        _settings = updated;
        _logger.LogInformation("Settings updated: {@Settings}", updated);

        if (pathsChanged)
        {
          _logger.LogInformation("Restarting watcher due to path changes.");
          RestartWatcher();
        }
      }
    });
  }

  protected override Task ExecuteAsync(CancellationToken stoppingToken)
  {
    StartWatcher(stoppingToken);
    return Task.CompletedTask;
  }

  private void StartWatcher(CancellationToken token)
  {
    lock (_sync)
    {
      _watcher?.Dispose();
      _watcher = null;

      if (string.IsNullOrWhiteSpace(_settings.WatchPath) ||
          string.IsNullOrWhiteSpace(_settings.DestinationPath))
      {
        _logger.LogWarning("Invalid WatchPath or DestinationPath. Watcher not started.");
        return;
      }

      Directory.CreateDirectory(_settings.WatchPath);
      Directory.CreateDirectory(_settings.DestinationPath);

      SortExistingFiles(_settings.WatchPath, _settings.DestinationPath);

      var watcher = new FileSystemWatcher(_settings.WatchPath)
      {
        IncludeSubdirectories = false,
        NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size
      };

      watcher.Created += async (s, e) =>
      {
        try
        {
          var extension = Path.GetExtension(e.FullPath).ToLowerInvariant();
          if (extension == ".tmp")
          {
            await Task.Delay(600000, token); // lets user select file location without failing to download
            SortFile(e.FullPath, _settings.DestinationPath);
          }
          else
          {
            await Task.Delay(5000, token); // let file finish installing
            SortFile(e.FullPath, _settings.DestinationPath);
          }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _logger.LogError(ex, "Error handling file {file}", e.FullPath); }
      };

      watcher.EnableRaisingEvents = true;
      _watcher = watcher;

      _logger.LogInformation("Watching {path}", _settings.WatchPath);
    }
  }

  private void RestartWatcher() => StartWatcher(CancellationToken.None);

  private void SortExistingFiles(string folder, string destPath)
  {
    foreach (var file in Directory.GetFiles(folder))
      SortFile(file, destPath);
  }

  private void SortFile(string filePath, string basePath)
  {
    try
    {
      if (!File.Exists(filePath)) return;

      string ext = Path.GetExtension(filePath).ToLowerInvariant();
      string folder = ext switch
      {
        ".jpg" or ".png" or ".gif" or ".svg" or ".jpeg" or ".bmp" or ".ico" or ".webp" => "Images",
        ".doc" or ".docx" or ".pdf" or ".txt" => "Text Documents",
        ".xlsx" or ".xls" or ".xlsb" or ".xlsm" => "Spreadsheets",
        ".mp3" or ".wav" or ".ogg" or ".aac" or ".alac" or ".flac" or ".m4a" => "Audio",
        ".mp4" or ".mov" or ".webm" => "Videos",
        ".exe" or ".dll" or ".bat" => "Executables",
        ".js" or ".html" or ".cs" or ".cpp" or ".c" or ".py" or ".sql" or ".xaml" or ".xml" or ".css" or ".lua" => "Code",
        ".lnk" => "Shortcuts",
        ".pptx" => "PowerPoint Presentations",
        _ => "Others"
      };

      var destDir = Path.Combine(basePath, folder);
      Directory.CreateDirectory(destDir);

      var destFile = Path.Combine(destDir, Path.GetFileName(filePath));
      File.Move(filePath, destFile, overwrite: true);

      _logger.LogInformation("Moved {src} --> {dst}", filePath, destFile);
    }
    catch (IOException ex) { _logger.LogError(ex, "File move failed for {file}", filePath); }
    catch (Exception ex) { _logger.LogError(ex, "Unexpected error for {file}", filePath); }
  }

  public override void Dispose()
  {
    lock (_sync)
    {
      _watcher?.Dispose();
      _watcher = null;
    }
    base.Dispose();
  }
}