using Microsoft.Win32;

namespace TetrisWindowsEdition.Modules;

/// <summary>
/// Item 15 do spec: "RESTAURAR WINDOWS". Aplica de volta um SystemSnapshot,
/// desfazendo exatamente o que os módulos do app têm permissão de mexer.
/// Idempotente: pode ser chamado duas vezes seguidas sem quebrar nada,
/// porque sempre escreve valores absolutos (nunca "incrementa" nada).
/// </summary>
public sealed class RestoreManager
{
    private readonly ChangeHistory _history;

    public RestoreManager(ChangeHistory history) => _history = history;

    public void Restore(SystemSnapshot snapshot)
    {
        RestoreWallpaper(snapshot);
        RestoreColors(snapshot);
        RestoreCursors(snapshot);
        RestoreSounds(snapshot);
        RestoreStartup(snapshot);
        RestoreScreensaver(snapshot);

        _history.Log("Restauração concluída", $"A partir do backup de {snapshot.CreatedUtc:u}");
    }

    private void RestoreWallpaper(SystemSnapshot s)
    {
        if (!string.IsNullOrEmpty(s.WallpaperPath) && File.Exists(s.WallpaperPath))
        {
            using var desktop = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: true);
            desktop?.SetValue("WallpaperStyle", s.WallpaperStyle.ToString());
            desktop?.SetValue("TileWallpaper", s.WallpaperTile.ToString());
            WallpaperModule.SetWallpaperFile(s.WallpaperPath);
        }
    }

    private void RestoreColors(SystemSnapshot s)
    {
        using var personalize = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", writable: true);
        if (s.SystemUsesLightTheme is int sys) personalize?.SetValue("SystemUsesLightTheme", sys);
        if (s.AppsUseLightTheme is int apps) personalize?.SetValue("AppsUseLightTheme", apps);
        if (s.EnableTransparency is int trans) personalize?.SetValue("EnableTransparency", trans);

        if (!string.IsNullOrEmpty(s.AccentColorArgb) &&
            int.TryParse(s.AccentColorArgb, System.Globalization.NumberStyles.HexNumber, null, out var accent))
        {
            using var dwm = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM", writable: true);
            dwm?.SetValue("AccentColor", accent, RegistryValueKind.DWord);
        }

        ColorsModule.BroadcastSettingChange();
    }

    private void RestoreCursors(SystemSnapshot s)
    {
        if (s.CursorScheme.Count == 0) return;
        using var cursors = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors", writable: true);
        if (cursors == null) return;

        foreach (var (name, value) in s.CursorScheme)
        {
            if (value == null) cursors.DeleteValue(name, throwOnMissingValue: false);
            else cursors.SetValue(name, value);
        }
        CursorsModule.ApplyCursorChange();
    }

    private void RestoreSounds(SystemSnapshot s)
    {
        foreach (var (eventName, value) in s.SoundEvents)
        {
            var keyPath = $@"AppEvents\Schemes\Apps\.Default\{eventName}\.Current";
            using var key = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);
            key?.SetValue(null, value ?? string.Empty);
        }
    }

    private void RestoreStartup(SystemSnapshot s)
    {
        if (s.RunOnStartup == 0)
            StartupModule.Disable();
        // Se era 1, deixamos como está (o próprio app é quem geria essa chave;
        // reativar aqui poderia reabrir um app que o usuário já desinstalou).
    }

    private void RestoreScreensaver(SystemSnapshot s)
    {
        using var desktop = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: true);
        if (desktop == null) return;

        if (string.IsNullOrEmpty(s.ScreensaverPath))
            desktop.DeleteValue("SCRNSAVE.EXE", throwOnMissingValue: false);
        else
            desktop.SetValue("SCRNSAVE.EXE", s.ScreensaverPath);

        if (s.ScreensaverTimeoutSeconds is int timeout)
            desktop.SetValue("ScreenSaveTimeOut", timeout.ToString());
        if (s.ScreensaverSecure is int secure)
            desktop.SetValue("ScreenSaverIsSecure", secure.ToString());
    }
}
