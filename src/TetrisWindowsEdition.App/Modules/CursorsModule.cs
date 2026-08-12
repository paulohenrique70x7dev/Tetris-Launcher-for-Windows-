using Microsoft.Win32;
using TetrisWindowsEdition.Native;

namespace TetrisWindowsEdition.Modules;

/// <summary>
/// Item 7 do spec: cursores temáticos. Cada valor nomeado abaixo é uma
/// entrada oficial de "Control Panel\Cursors" — a mesma chave que o painel
/// Configurações > Dispositivos > Mouse > Cursores adicionais grava.
/// Não substitui os arquivos .cur do Windows: aponta para os nossos.
/// </summary>
public static class CursorsModule
{
    // Nome do valor de registro -> arquivo .cur/.ani esperado em Resources\Cursors
    public static readonly IReadOnlyDictionary<string, string> Slots = new Dictionary<string, string>
    {
        ["Arrow"] = "tetris_arrow.cur",
        ["Help"] = "tetris_help.cur",
        // AppStarting/Wait ficam como .cur estático por enquanto — cursores
        // animados exigem o formato .ani (RIFF), que pode ser adicionado
        // depois sem mudar esta estrutura, só trocando a extensão aqui.
        ["AppStarting"] = "tetris_appstarting.cur",
        ["Wait"] = "tetris_wait.cur",
        ["Crosshair"] = "tetris_precision.cur",
        ["IBeam"] = "tetris_ibeam.cur",
        ["NWPen"] = "tetris_handwriting.cur",
        ["No"] = "tetris_unavailable.cur",
        ["SizeWE"] = "tetris_sizewe.cur",
        ["SizeNS"] = "tetris_sizens.cur",
        ["SizeNWSE"] = "tetris_sizenwse.cur",
        ["SizeAll"] = "tetris_move.cur",
        ["UpArrow"] = "tetris_altselect.cur",
        ["Hand"] = "tetris_link.cur",
    };

    public static void Apply(string cursorsFolder)
    {
        using var cursors = Registry.CurrentUser.CreateSubKey(@"Control Panel\Cursors", writable: true);
        if (cursors == null) return;

        cursors.SetValue(string.Empty, "Tetris Windows Edition");

        foreach (var (slot, fileName) in Slots)
        {
            var fullPath = Path.Combine(cursorsFolder, fileName);
            if (File.Exists(fullPath))
                cursors.SetValue(slot, fullPath);
        }

        ApplyCursorChange();
    }

    public static void RestoreWindowsDefault()
    {
        using var cursors = Registry.CurrentUser.CreateSubKey(@"Control Panel\Cursors", writable: true);
        cursors?.SetValue(string.Empty, "Windows Default");
        foreach (var slot in Slots.Keys)
            cursors?.DeleteValue(slot, throwOnMissingValue: false);
        ApplyCursorChange();
    }

    public static void ApplyCursorChange() =>
        NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETCURSORS, 0, IntPtr.Zero,
            NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE);
}
