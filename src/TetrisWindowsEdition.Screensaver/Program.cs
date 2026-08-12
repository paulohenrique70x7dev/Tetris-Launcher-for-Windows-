using System.Windows.Forms;

namespace TetrisWindowsEdition.Screensaver;

/// <summary>
/// Ponto de entrada. Segue a convenção padrão do Windows para .scr:
///   /s          -> roda a proteção de tela em tela cheia
///   /c ou /c:N  -> mostra a caixa de configuração
///   /p N        -> modo miniatura de pré-visualização, embutido na janela N
/// Sem argumentos, o Windows normalmente chama com /c.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var mode = ParseMode(args, out var previewHandle);

        switch (mode)
        {
            case Mode.Preview:
                Application.Run(new ScreensaverForm(preview: true, previewParentHandle: previewHandle));
                break;

            case Mode.Config:
                Application.Run(new ConfigForm());
                break;

            case Mode.Show:
            default:
                Application.Run(new ScreensaverForm(preview: false, previewParentHandle: IntPtr.Zero));
                break;
        }
    }

    private enum Mode { Show, Config, Preview }

    private static Mode ParseMode(string[] args, out IntPtr previewHandle)
    {
        previewHandle = IntPtr.Zero;
        if (args.Length == 0) return Mode.Config;

        var first = args[0].Trim().ToLowerInvariant();

        if (first.StartsWith("/s")) return Mode.Show;
        if (first.StartsWith("/c")) return Mode.Config;

        if (first.StartsWith("/p"))
        {
            var handleArg = args.Length > 1 ? args[1] : first.Split(':').ElementAtOrDefault(1);
            if (long.TryParse(handleArg, out var handleValue))
                previewHandle = new IntPtr(handleValue);
            return Mode.Preview;
        }

        return Mode.Config;
    }
}
