using Microsoft.Win32;

namespace TetrisWindowsEdition.Modules;

/// <summary>
/// Item 8 do spec: pacote de sons Tetris. Os arquivos .wav em
/// Resources\Sounds são originais/sintetizados especificamente para o
/// projeto (nunca gravações protegidas por direitos autorais).
/// Cada evento é uma chave oficial em AppEvents\Schemes\Apps\.Default.
/// </summary>
public static class SoundsModule
{
    // Nome do evento do Windows -> arquivo .wav esperado
    public static readonly IReadOnlyDictionary<string, string> EventFiles = new Dictionary<string, string>
    {
        ["SystemStart"] = "tetris_startup.wav",
        ["SystemExit"] = "tetris_shutdown.wav",
        ["Notification.Default"] = "tetris_notify.wav",
        ["SystemHand"] = "tetris_error.wav",
        ["SystemExclamation"] = "tetris_warning.wav",
        ["DeviceConnect"] = "tetris_device_connect.wav",
        ["DeviceDisconnect"] = "tetris_device_disconnect.wav",
        ["Notification.Mail"] = "tetris_new_message.wav",
        ["Notification.Reminder"] = "tetris_reminder.wav",
        ["SystemAsterisk"] = "tetris_system_event.wav",
    };

    public static IReadOnlyList<string> KnownEventSchemeKeys => EventFiles.Keys.ToList();

    public static void Apply(string soundsFolder, IReadOnlySet<string>? enabledEvents = null)
    {
        foreach (var (eventName, fileName) in EventFiles)
        {
            var enabled = enabledEvents == null || enabledEvents.Contains(eventName);
            var fullPath = Path.Combine(soundsFolder, fileName);
            var keyPath = $@"AppEvents\Schemes\Apps\.Default\{eventName}\.Current";

            using var key = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);
            key?.SetValue(null, enabled && File.Exists(fullPath) ? fullPath : string.Empty);
        }
    }

    public static void RestoreSilence()
    {
        foreach (var eventName in EventFiles.Keys)
        {
            var keyPath = $@"AppEvents\Schemes\Apps\.Default\{eventName}\.Current";
            using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
            key?.SetValue(null, string.Empty);
        }
    }
}
