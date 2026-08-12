namespace TetrisWindowsEdition.Modules;

public enum SupportLevel { FullySupported, Partial, Unsupported }

public sealed record CompatibilityItem(string Feature, SupportLevel Level, string Explanation);

/// <summary>
/// Item 21 do spec: "ANÁLISE DO SEU WINDOWS", exibida antes de aplicar
/// qualquer tema. Nunca finge que algo funciona quando o Windows não permite.
/// </summary>
public static class CompatibilityAnalyzer
{
    public static IReadOnlyList<CompatibilityItem> Analyze(EnvironmentReport env)
    {
        var items = new List<CompatibilityItem>
        {
            new("Papel de parede", SupportLevel.FullySupported,
                "API oficial do Windows (SystemParametersInfo)."),

            new("Wallpaper animado (peças caindo)", SupportLevel.FullySupported,
                "Hospedado atrás dos ícones via WorkerW, sem tocar em arquivos do sistema."),

            new("Cores de destaque / modo escuro", SupportLevel.FullySupported,
                "Chaves de Registro oficiais de Personalização (as mesmas do painel de Configurações)."),

            new("Cursores do mouse", SupportLevel.FullySupported,
                "Esquema de cursores via Control Panel\\Cursors, reversível."),

            new("Sons do sistema", SupportLevel.FullySupported,
                "Esquema de sons via AppEvents, com arquivos .wav originais do projeto."),

            new("Proteção de tela", SupportLevel.FullySupported,
                "Executável .scr próprio, registrado via SCRNSAVE.EXE."),

            new("Inicialização automática do app", SupportLevel.FullySupported,
                "HKCU\\...\\Run, sem privilégios de administrador."),

            new("Tela de bloqueio", env.IsProOrEnterprise ? SupportLevel.Partial : SupportLevel.Unsupported,
                env.IsProOrEnterprise
                    ? "Disponível via política de grupo (exige administrador uma única vez)."
                    : "Windows Home não expõe API pública para isso. Alternativa: trocar manualmente em Configurações > Personalização > Tela de bloqueio."),

            new("Posição da barra de tarefas", SupportLevel.Unsupported,
                "Windows 11 removeu o suporte a reposicionar a barra de tarefas; não existe API oficial atual."),

            new("Ícones de executáveis do sistema (explorer.exe, etc.)", SupportLevel.Unsupported,
                "Alterar isso exigiria modificar arquivos protegidos do sistema — o app nunca faz isso."),
        };

        return items;
    }
}
