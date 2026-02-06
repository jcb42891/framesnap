using System.IO;
using System.Text.Json;
using FrameSnap.Core;

namespace FrameSnap.Settings;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public SettingsStore()
    {
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _settingsPath = Path.Combine(appDataFolder, "FrameSnap", "settings.json");
    }

    public CaptureSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new CaptureSettings();
            }

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<CaptureSettings>(json, SerializerOptions);
            return settings ?? new CaptureSettings();
        }
        catch
        {
            return new CaptureSettings();
        }
    }

    public void Save(CaptureSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        File.WriteAllText(_settingsPath, json);
    }
}
