using System.Diagnostics;
using Microsoft.Win32;

namespace TetrisWindowsEdition.Modules;

public enum LockScreenSupport { FullySupported, PolicyBasedRequiresAdmin, Unsupported }

/// <summary>
/// Item 9 do spec: tela de bloqueio. É a personalização mais limitada do
/// Windows, e a documentamos honestamente em vez de fingir que funciona
/// (item 21/26 do spec):
///
///  • Windows 10/11 HOME: a Microsoft não expõe API pública nem política
///    de grupo para trocar a imagem da tela de bloqueio programaticamente.
///    Único caminho é o usuário trocar manualmente em Configurações.
///    -> Unsupported (mostramos o botão "Abrir Configurações de Bloqueio").
///
///  • Windows 10/11 PRO/ENTERPRISE/EDUCATION: existe a política de grupo
///    "Force a specific default lock screen image", que corresponde à
///    chave HKLM\SOFTWARE\Policies\Microsoft\Windows\Personalization.
///    Isso EXIGE admin (é HKLM) e é reversível (removendo a chave).
///    -> PolicyBasedRequiresAdmin.
/// </summary>
public static class LockScreenModule
{
    private const string PolicyKeyPath = @"SOFTWARE\Policies\Microsoft\Windows\Personalization";

    public static LockScreenSupport DetectSupport(EnvironmentReport env) =>
        env.IsProOrEnterprise ? LockScreenSupport.PolicyBasedRequiresAdmin : LockScreenSupport.Unsupported;

    /// <summary>
    /// Dispara um processo elevado à parte (nunca o app inteiro) para gravar
    /// a chave HKLM necessária. Pede confirmação explícita ao usuário antes
    /// (UAC já cuida disso, mas o app mostra o motivo antes de chamar).
    /// </summary>
    public static bool ApplyViaPolicy(string imagePath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "reg.exe",
            Arguments = $"add \"HKLM\\{PolicyKeyPath}\" /v LockScreenImage /t REG_SZ /d \"{imagePath}\" /f",
            Verb = "runas",
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            using var process = Process.Start(psi);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Usuário cancelou o prompt do UAC.
            return false;
        }
    }

    public static bool RemovePolicy()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "reg.exe",
            Arguments = $"delete \"HKLM\\{PolicyKeyPath}\" /v LockScreenImage /f",
            Verb = "runas",
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            using var process = Process.Start(psi);
            process?.WaitForExit();
            return true;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
