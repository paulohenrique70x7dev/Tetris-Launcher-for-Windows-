using Microsoft.Win32;
using System.IO;
namespace TetrisWindowsEdition.Modules;

/// <summary>
/// Item 10 do spec: proteção de tela. O executável real (peças caindo
/// formando linhas) vive no projeto irmão TetrisWindowsEdition.Screensaver,
/// compilado como .scr — esse módulo só registra/desregistra o .scr
/// nas chaves oficiais que o Windows lê para "Configurações > Proteção de tela".
/// </summary>
public static class ScreensaverModule
{
    private const string DesktopKeyPath = @"Control Panel\Desktop";

    public static void Register(string scrFullPath, int timeoutSeconds = 300, bool requirePasswordOnResume = true)
    {
        if (!File.Exists(scrFullPath))
            throw new FileNotFoundException("Arquivo .scr não encontrado.", scrFullPath);

        using var desktop = Registry.CurrentUser.CreateSubKey(DesktopKeyPath, writable: true);
        desktop?.SetValue("SCRNSAVE.EXE", scrFullPath);
        desktop?.SetValue("ScreenSaveActive", "1");
        desktop?.SetValue("ScreenSaveTimeOut", timeoutSeconds.ToString());
        desktop?.SetValue("ScreenSaverIsSecure", requirePasswordOnResume ? "1" : "0");
    }

    public static void Unregister()
    {
        using var desktop = Registry.CurrentUser.OpenSubKey(DesktopKeyPath, writable: true);
        desktop?.DeleteValue("SCRNSAVE.EXE", throwOnMissingValue: false);
        desktop?.SetValue("ScreenSaveActive", "0");
    }

    public static void UpdateSettings(int speed, int pieceCount, int size, string colorMode, bool soundsEnabled)
    {
        // A própria .scr lê estes valores num arquivo de config próprio
        // (não no Registro do Windows), porque não são parâmetros que o
        // shell do Windows entende — são específicos do nosso protetor.
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TetrisWindowsEdition", "screensaver_config.json");

        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            speed,
            pieceCount,
            size,
            colorMode,
            soundsEnabled
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(configPath, json);
    }
}
