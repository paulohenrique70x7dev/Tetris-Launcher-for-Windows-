using Microsoft.Win32;
using TetrisWindowsEdition.Native;

namespace TetrisWindowsEdition.Modules;

public enum WallpaperFit { Fill = 10, Fit = 6, Stretch = 2, Tile = 0, Center = 0, Span = 22 }

/// <summary>
/// Item 5 do spec: papel de parede estático via API oficial (SystemParametersInfo).
/// O wallpaper animado (peças caindo) é tratado à parte por WallpaperLiveHost,
/// que reaproveita a técnica WPF+WebView2+WorkerW já validada no protótipo anterior.
/// </summary>
public static class WallpaperModule
{
    public static void SetWallpaperFile(string absolutePath, WallpaperFit fit = WallpaperFit.Fill)
    {
        if (!File.Exists(absolutePath))
            throw new FileNotFoundException("Wallpaper não encontrado", absolutePath);

        using (var desktop = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: true))
        {
            desktop?.SetValue("WallpaperStyle", ((int)fit).ToString());
            desktop?.SetValue("TileWallpaper", fit == WallpaperFit.Tile ? "1" : "0");
        }

        NativeMethods.SystemParametersInfo(
            NativeMethods.SPI_SETDESKWALLPAPER, 0, absolutePath,
            NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE);
    }
}
