using System.Runtime.InteropServices;
using System.IO;

namespace TetrisWindowsEdition.Modules;

/// <summary>
/// Item 11 do spec: ícones e atalhos. IMPORTANTE — nunca tocamos nos
/// executáveis originais do Windows. Só criamos/alteramos atalhos .lnk
/// que já pertencem ao próprio app ou que o usuário aponta explicitamente
/// (ex.: um atalho na Área de Trabalho dele).
/// </summary>
public static class IconsModule
{
    public static void SetShortcutIcon(string shortcutLnkPath, string iconIcoPath)
    {
        if (!File.Exists(shortcutLnkPath))
            throw new FileNotFoundException("Atalho não encontrado.", shortcutLnkPath);
        if (!File.Exists(iconIcoPath))
            throw new FileNotFoundException("Ícone não encontrado.", iconIcoPath);

        dynamic shell = Activator.CreateInstance(
            Type.GetTypeFromProgID("WScript.Shell")!)!;
        dynamic shortcut = shell.CreateShortcut(shortcutLnkPath);
        shortcut.IconLocation = iconIcoPath + ",0";
        shortcut.Save();
        Marshal.ReleaseComObject(shortcut);
        Marshal.ReleaseComObject(shell);
    }

    public static string CreateThemedDesktopShortcut(string targetExePath, string shortcutName, string iconIcoPath)
    {
        var desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var lnkPath = Path.Combine(desktopFolder, $"{shortcutName}.lnk");

        dynamic shell = Activator.CreateInstance(
            Type.GetTypeFromProgID("WScript.Shell")!)!;
        dynamic shortcut = shell.CreateShortcut(lnkPath);
        shortcut.TargetPath = targetExePath;
        shortcut.IconLocation = iconIcoPath + ",0";
        shortcut.WorkingDirectory = Path.GetDirectoryName(targetExePath);
        shortcut.Save();
        Marshal.ReleaseComObject(shortcut);
        Marshal.ReleaseComObject(shell);

        return lnkPath;
    }
}
