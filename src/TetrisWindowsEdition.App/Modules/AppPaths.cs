namespace TetrisWindowsEdition.Modules;

/// <summary>
/// Centraliza todos os caminhos usados pelo app, para nunca espalhar
/// strings de pasta pelo código (e para facilitar limpeza na desinstalação).
/// </summary>
public static class AppPaths
{
    public static string DataRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TetrisWindowsEdition");

    public static string BackupsFolder => Path.Combine(DataRoot, "Backups");
    public static string HistoryFile => Path.Combine(DataRoot, "history.json");
    public static string SettingsFile => Path.Combine(DataRoot, "settings.json");
    public static string ExportedThemesFolder => Path.Combine(DataRoot, "ExportedThemes");

    public static string ResourcesRoot { get; } = Path.Combine(
        AppContext.BaseDirectory, "Resources");

    public static string WallpapersFolder => Path.Combine(ResourcesRoot, "Wallpapers");
    public static string CursorsFolder => Path.Combine(ResourcesRoot, "Cursors");
    public static string SoundsFolder => Path.Combine(ResourcesRoot, "Sounds");
    public static string IconsFolder => Path.Combine(ResourcesRoot, "Icons");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(DataRoot);
        Directory.CreateDirectory(BackupsFolder);
        Directory.CreateDirectory(ExportedThemesFolder);
    }
}
