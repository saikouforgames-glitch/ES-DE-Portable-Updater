using System.Text.Json;

namespace ESDEUpdater;

public static class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static string SettingsPath =>
        Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            Diagnostics.Report($"Could not read settings from {SettingsPath}: {ex.Message}");
            return new AppSettings();
        }
    }

    public static bool Save(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
            return true;
        }
        catch (Exception ex)
        {
            Diagnostics.Report($"Could not save settings to {SettingsPath}: {ex.Message}");
            return false;
        }
    }
}
