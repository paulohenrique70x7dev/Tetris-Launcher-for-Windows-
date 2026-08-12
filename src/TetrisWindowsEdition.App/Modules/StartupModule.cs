using Microsoft.Win32;
using System.Reflection;

namespace TetrisWindowsEdition.Modules;

/// <summary>
/// Item 19 do spec (iniciar com o Windows) — via HKCU\...\Run, sem
/// privilégios de administrador e sem tarefa agendada. Reversível com
/// um simples DeleteValue.
/// </summary>
public static class StartupModule
{
    public const string RunValueName = "TetrisWindowsEdition";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void Enable()
    {
        var exePath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        key?.SetValue(RunValueName, $"\"{exePath}\" --minimized");
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(RunValueName, throwOnMissingValue: false);
    }

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(RunValueName) != null;
    }
}
