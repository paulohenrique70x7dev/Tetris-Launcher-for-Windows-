using System.Windows;
using System.Windows.Interop;
using TetrisWindowsEdition.Native;

namespace TetrisWindowsEdition.Modules;

/// <summary>
/// Hospeda o wallpaper animado HTML (peças de Tetris caindo) atrás dos ícones
/// da área de trabalho, reaproveitando a técnica já validada no protótipo:
/// uma janela WPF sem borda é reparentada para dentro do WorkerW que o
/// Explorer cria abaixo de Progman quando pedimos para desenhar o fundo.
///
/// Esta classe só cuida do posicionamento de janela (Win32 interop).
/// O conteúdo em si (HTML/WebView2) fica na WallpaperWindow (Views).
/// </summary>
public static class WallpaperLiveHost
{
    public static bool TryAttachToDesktop(Window wallpaperWindow)
    {
        var progman = NativeMethods.FindWindow("Progman", null);
        if (progman == IntPtr.Zero) return false;

        // Pede ao Progman para criar um WorkerW atrás dos ícones (comportamento
        // documentado, usado por diversos softwares de wallpaper dinâmico).
        NativeMethods.SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero,
            NativeMethods.SMTO_ABORTIFHUNG, 1000, out _);

        IntPtr workerW = IntPtr.Zero;
        NativeMethods.EnumWindows((hwnd, _) =>
        {
            var shellView = NativeMethods.FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView != IntPtr.Zero)
            {
                workerW = NativeMethods.FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
            }
            return true;
        }, IntPtr.Zero);

        if (workerW == IntPtr.Zero) return false;

        var hwndSource = (HwndSource)PresentationSource.FromVisual(wallpaperWindow);
        NativeMethods.SetParent(hwndSource.Handle, workerW);
        return true;
    }
}
