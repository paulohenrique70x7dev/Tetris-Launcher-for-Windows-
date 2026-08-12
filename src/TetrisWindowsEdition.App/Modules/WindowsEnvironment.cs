using Microsoft.Win32;

namespace TetrisWindowsEdition.Modules;

public enum WindowsGeneration { Windows10, Windows11, Unsupported }

public sealed record EnvironmentReport(
    WindowsGeneration Generation,
    string EditionName,
    string DisplayVersion,
    int BuildNumber,
    bool Is64Bit,
    bool IsProOrEnterprise);

/// <summary>
/// Item 16 do spec: detecção de Windows 10/11, edição e arquitetura,
/// sem depender de internet. Lê apenas chaves de leitura pública do
/// Registro (CurrentVersion), nunca grava nada aqui.
/// </summary>
public static class WindowsEnvironment
{
    private const string CurrentVersionKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

    public static EnvironmentReport Detect()
    {
        using var key = Registry.LocalMachine.OpenSubKey(CurrentVersionKey);

        int buildNumber = 0;
        if (key?.GetValue("CurrentBuildNumber") is string buildStr)
            int.TryParse(buildStr, out buildNumber);

        string editionName = key?.GetValue("EditionID") as string ?? "Desconhecida";
        string displayVersion = key?.GetValue("DisplayVersion") as string
                                  ?? key?.GetValue("ReleaseId") as string
                                  ?? "?";

        // A partir do build 22000 o Windows passou a se identificar como Windows 11.
        var generation = buildNumber switch
        {
            >= 22000 => WindowsGeneration.Windows11,
            >= 10240 => WindowsGeneration.Windows10,
            _ => WindowsGeneration.Unsupported
        };

        bool isProOrEnterprise = editionName.Contains("Pro", StringComparison.OrdinalIgnoreCase)
                                  || editionName.Contains("Enterprise", StringComparison.OrdinalIgnoreCase)
                                  || editionName.Contains("Education", StringComparison.OrdinalIgnoreCase);

        return new EnvironmentReport(
            Generation: generation,
            EditionName: editionName,
            DisplayVersion: displayVersion,
            BuildNumber: buildNumber,
            Is64Bit: Environment.Is64BitOperatingSystem,
            IsProOrEnterprise: isProOrEnterprise);
    }
}
