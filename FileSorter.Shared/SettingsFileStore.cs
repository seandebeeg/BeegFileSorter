using System.Text.Json;

namespace FileSorter.Shared
{
  public sealed class SettingsFileStore
  {
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
      WriteIndented = true
    };

    public SettingsFileStore()
    {
      var baseDir = Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
          "FileSorter");

      Directory.CreateDirectory(baseDir);

      _configPath = Path.Combine(baseDir, "appsettings.json");

      if (!File.Exists(_configPath))
      {
        Save(new FileSortSettings
        {
          WatchPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
          DestinationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Sorted")
        });
      }
    }

    public string ConfigPath => _configPath;

    public FileSortSettings Load()
    {
      if (!File.Exists(_configPath))
        throw new FileNotFoundException("Settings file not found.", _configPath);

      var json = File.ReadAllText(_configPath);
      using var doc = JsonDocument.Parse(json);

      if (!doc.RootElement.TryGetProperty("FileSort", out var section))
        return new FileSortSettings();

      return new FileSortSettings
      {
        WatchPath = section.GetProperty("WatchPath").GetString() ?? "",
        DestinationPath = section.GetProperty("DestinationPath").GetString() ?? ""
      };
    }

    public void Save(FileSortSettings settings)
    {
      var wrapper = new
      {
        FileSort = settings
      };

      var json = JsonSerializer.Serialize(wrapper, _jsonOptions);
      File.WriteAllText(_configPath, json);
    }
  }
}
