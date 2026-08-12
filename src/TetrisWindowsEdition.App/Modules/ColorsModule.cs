using Microsoft.Win32;
using TetrisWindowsEdition.Native;

namespace TetrisWindowsEdition.Modules;

public sealed record ColorScheme(
    string Name,
    string AccentColorHex,   // "#RRGGBB"
    bool DarkMode,
    bool Transparency);

/// <summary>
/// Item 6 do spec: os 5 esquemas de cor do tema Tetris.
/// Usa apenas as chaves de Registro oficiais de personalização
/// (as mesmas que a tela Configurações > Personalização > Cores grava).
/// </summary>
public static class ColorSchemes
{
    public static readonly ColorScheme TetrisClassic = new("Tetris Classic", "#2E7D32", true, false);
    public static readonly ColorScheme TetrisNeon = new("Tetris Neon", "#00E5FF", true, true);
    public static readonly ColorScheme TetrisDark = new("Tetris Dark", "#616161", true, false);
    public static readonly ColorScheme TetrisGameBoy = new("Tetris Game Boy", "#8BAC0F", false, false);
    public static readonly ColorScheme TetrisRainbow = new("Tetris Rainbow", "#E040FB", true, true);

    public static IReadOnlyList<ColorScheme> All { get; } =
        new[] { TetrisClassic, TetrisNeon, TetrisDark, TetrisGameBoy, TetrisRainbow };
}

public static class ColorsModule
{
    public static void Apply(ColorScheme scheme)
    {
        var argb = ToAbgrInt(scheme.AccentColorHex);

        using (var dwm = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\DWM", writable: true))
        {
            dwm?.SetValue("AccentColor", argb, RegistryValueKind.DWord);
            dwm?.SetValue("ColorPrevalence", 1, RegistryValueKind.DWord);
        }

        using (var personalize = Registry.CurrentUser.CreateSubKey(
                   @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", writable: true))
        {
            personalize?.SetValue("SystemUsesLightTheme", scheme.DarkMode ? 0 : 1, RegistryValueKind.DWord);
            personalize?.SetValue("AppsUseLightTheme", scheme.DarkMode ? 0 : 1, RegistryValueKind.DWord);
            personalize?.SetValue("ColorPrevalence", 1, RegistryValueKind.DWord);
            personalize?.SetValue("EnableTransparency", scheme.Transparency ? 1 : 0, RegistryValueKind.DWord);
        }

        BroadcastSettingChange();
    }

    /// <summary>
    /// O Windows guarda AccentColor no formato 0xAABBGGRR (ABGR), não no
    /// RGB comum. Essa conversão é o detalhe que mais gente erra ao tentar
    /// mudar a cor de destaque por Registro.
    /// </summary>
    private static int ToAbgrInt(string hexRgb)
    {
        var hex = hexRgb.TrimStart('#');
        byte r = Convert.ToByte(hex[..2], 16);
        byte g = Convert.ToByte(hex[2..4], 16);
        byte b = Convert.ToByte(hex[4..6], 16);
        byte a = 0xFF;
        return (a << 24) | (b << 16) | (g << 8) | r;
    }

    public static void BroadcastSettingChange()
    {
        NativeMethods.SendMessageTimeout(
            (IntPtr)NativeMethods.HWND_BROADCAST, NativeMethods.WM_SETTINGCHANGE,
            UIntPtr.Zero, "ImmersiveColorSet",
            NativeMethods.SMTO_ABORTIFHUNG, 1000, out _);

        NativeMethods.SendMessageTimeout(
            (IntPtr)NativeMethods.HWND_BROADCAST, NativeMethods.WM_SYSCOLORCHANGE,
            UIntPtr.Zero, string.Empty,
            NativeMethods.SMTO_ABORTIFHUNG, 1000, out _);
    }
}
