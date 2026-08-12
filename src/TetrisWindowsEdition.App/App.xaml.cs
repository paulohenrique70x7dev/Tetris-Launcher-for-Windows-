using System.Windows;
using TetrisWindowsEdition.Modules;

namespace TetrisWindowsEdition;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppPaths.EnsureCreated();

        // --minimized: usado quando o Windows abre o app junto com o login
        // (StartupModule.Enable grava esse argumento). Nesse caso o app
        // nasce só na bandeja, sem roubar foco do usuário.
        if (e.Args.Contains("--minimized"))
        {
            MainWindow?.Hide();
        }
    }
}
