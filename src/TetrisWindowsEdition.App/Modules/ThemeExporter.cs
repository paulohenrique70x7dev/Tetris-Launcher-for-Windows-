using System.Text;

namespace TetrisWindowsEdition.Modules;

/// <summary>
/// Item 13 do spec: gera um pacote de tema .theme real do Windows,
/// que o usuário pode clicar duas vezes para aplicar (ou compartilhar).
/// Formato .theme é um INI documentado, suportado nativamente pelo Explorer.
/// </summary>
public static class ThemeExporter
{
    public static string Export(string exportFolder, string themeName, ColorScheme scheme, string wallpaperPath)
    {
        Directory.CreateDirectory(exportFolder);
        var themePath = Path.Combine(exportFolder, $"{SanitizeFileName(themeName)}.theme");

        var sb = new StringBuilder();
        sb.AppendLine("[Theme]");
        sb.AppendLine("DisplayName=" + themeName);
        sb.AppendLine();
        sb.AppendLine("[Control Panel\\Desktop]");
        sb.AppendLine("Wallpaper=" + wallpaperPath);
        sb.AppendLine("WallpaperStyle=10");
        sb.AppendLine("TileWallpaper=0");
        sb.AppendLine();
        sb.AppendLine("[VisualStyles]");
        sb.AppendLine("Path=%SystemRoot%\\resources\\Themes\\Aero\\Aero.msstyles");
        sb.AppendLine("ColorStyle=NormalColor");
        sb.AppendLine("Size=NormalSize");
        sb.AppendLine();
        sb.AppendLine("[MasterThemeSelector]");
        sb.AppendLine("MTSM=" + (scheme.DarkMode ? "DABJDXBAAAA" : "DABJAAAAAAA"));
        sb.AppendLine();
        sb.AppendLine("[Control Panel\\Colors]");
        sb.AppendLine("Hilight=" + HexToDecTriplet(scheme.AccentColorHex));

        File.WriteAllText(themePath, sb.ToString(), Encoding.UTF8);
        return themePath;
    }

    private static string HexToDecTriplet(string hex)
    {
        hex = hex.TrimStart('#');
        var r = Convert.ToInt32(hex[..2], 16);
        var g = Convert.ToInt32(hex[2..4], 16);
        var b = Convert.ToInt32(hex[4..6], 16);
        return $"{r} {g} {b}";
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');
        return name;
    }
}
