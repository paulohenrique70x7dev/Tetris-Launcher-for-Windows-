using System.Text.Json;

namespace TetrisWindowsEdition.Screensaver;

public sealed class ScreensaverConfig
{
    public int Speed { get; set; } = 5;
    public int PieceCount { get; set; } = 12;
    public int Size { get; set; } = 28;
    public string ColorMode { get; set; } = "classic"; // classic | neon | monochrome
    public bool SoundsEnabled { get; set; } = false;

    public static ScreensaverConfig Load()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TetrisWindowsEdition", "screensaver_config.json");

        if (!File.Exists(path)) return new ScreensaverConfig();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ScreensaverConfig>(json) ?? new ScreensaverConfig();
        }
        catch
        {
            return new ScreensaverConfig();
        }
    }
}
