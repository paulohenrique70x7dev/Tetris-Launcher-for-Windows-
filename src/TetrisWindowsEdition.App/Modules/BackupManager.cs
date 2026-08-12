using System.Text.Json;
using Microsoft.Win32;

namespace TetrisWindowsEdition.Modules;

/// <summary>
/// Estado original do Windows, capturado ANTES de qualquer alteração.
/// Guardamos só o que o app pode vir a mexer — nunca um dump do Registro inteiro.
/// </summary>
public sealed class SystemSnapshot
{
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string? WallpaperPath { get; set; }
    public int WallpaperStyle { get; set; }
    public int WallpaperTile { get; set; }
    public string? AccentColorArgb { get; set; }
    public int? SystemUsesLightTheme { get; set; }
    public int? AppsUseLightTheme { get; set; }
    public int? EnableTransparency { get; set; }
    public Dictionary<string, string?> CursorScheme { get; set; } = new();
    public Dictionary<string, string?> SoundEvents { get; set; } = new();
    public int? RunOnStartup { get; set; }
    public string? ScreensaverPath { get; set; }
    public int? ScreensaverTimeoutSeconds { get; set; }
    public int? ScreensaverSecure { get; set; }
}

/// <summary>
/// Item 4 do spec ("Central de Segurança"): cria e gerencia backups do
/// estado original ANTES de qualquer alteração feita pelo app.
/// Cada backup vira um arquivo JSON com timestamp na pasta de dados do app.
/// </summary>
public sealed class BackupManager
{
    private readonly string _backupFolder;
    private readonly ChangeHistory _history;

    public BackupManager(string backupFolder, ChangeHistory history)
    {
        _backupFolder = backupFolder;
        _history = history;
        Directory.CreateDirectory(_backupFolder);
    }

    public IEnumerable<string> ListBackups() =>
        Directory.Exists(_backupFolder)
            ? Directory.GetFiles(_backupFolder, "backup_*.json").OrderByDescending(f => f)
            : Enumerable.Empty<string>();

    /// <summary>
    /// Captura o estado atual do Windows relevante aos módulos do app.
    /// Chamado automaticamente antes de "Aplicar Tema" e disponível como
    /// botão manual "Criar Backup".
    /// </summary>
    public string CreateBackup(string label = "manual")
    {
        var snapshot = CaptureCurrentState();

        var fileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}_{label}.json";
        var fullPath = Path.Combine(_backupFolder, fileName);

        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(fullPath, json);

        _history.Log("Backup criado", fileName);
        return fullPath;
    }

    public SystemSnapshot LoadBackup(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SystemSnapshot>(json)
               ?? throw new InvalidDataException("Arquivo de backup inválido ou corrompido.");
    }

    private static SystemSnapshot CaptureCurrentState()
    {
        var snapshot = new SystemSnapshot();

        using (var desktop = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
        {
            snapshot.WallpaperPath = desktop?.GetValue("WallPaper") as string;
            if (int.TryParse(desktop?.GetValue("WallpaperStyle") as string, out var style))
                snapshot.WallpaperStyle = style;
            if (int.TryParse(desktop?.GetValue("TileWallpaper") as string, out var tile))
                snapshot.WallpaperTile = tile;
        }

        using (var personalize = Registry.CurrentUser.OpenSubKey(
                   @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
        {
            snapshot.SystemUsesLightTheme = personalize?.GetValue("SystemUsesLightTheme") as int?;
            snapshot.AppsUseLightTheme = personalize?.GetValue("AppsUseLightTheme") as int?;
            snapshot.EnableTransparency = personalize?.GetValue("EnableTransparency") as int?;
        }

        using (var dwm = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM"))
        {
            if (dwm?.GetValue("AccentColor") is int accentColorInt)
                snapshot.AccentColorArgb = accentColorInt.ToString("X8");
        }

        using (var cursors = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors"))
        {
            if (cursors != null)
            {
                foreach (var valueName in cursors.GetValueNames())
                    snapshot.CursorScheme[valueName] = cursors.GetValue(valueName) as string;
            }
        }

        foreach (var eventName in SoundsModule.KnownEventSchemeKeys)
        {
            using var eventKey = Registry.CurrentUser.OpenSubKey(
                $@"AppEvents\Schemes\Apps\.Default\{eventName}\.Current");
            snapshot.SoundEvents[eventName] = eventKey?.GetValue(null) as string;
        }

        using (var run = Registry.CurrentUser.OpenSubKey(
                   @"Software\Microsoft\Windows\CurrentVersion\Run"))
        {
            snapshot.RunOnStartup = run?.GetValue(StartupModule.RunValueName) != null ? 1 : 0;
        }

        using (var scr = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
        {
            snapshot.ScreensaverPath = scr?.GetValue("SCRNSAVE.EXE") as string;
            if (int.TryParse(scr?.GetValue("ScreenSaveTimeOut") as string, out var timeout))
                snapshot.ScreensaverTimeoutSeconds = timeout;
            if (int.TryParse(scr?.GetValue("ScreenSaverIsSecure") as string, out var secure))
                snapshot.ScreensaverSecure = secure;
        }

        return snapshot;
    }
}
