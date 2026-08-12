using System.IO;
using System.Windows;
using Microsoft.Win32;
using TetrisWindowsEdition.Modules;

namespace TetrisWindowsEdition;

public partial class MainWindow : Window
{
    private readonly ChangeHistory _history;
    private readonly BackupManager _backupManager;
    private readonly RestoreManager _restoreManager;
    private readonly EnvironmentReport _env;

    private ColorScheme _selectedScheme = ColorSchemes.TetrisClassic;
    private string? _customWallpaperPath;

    public MainWindow()
    {
        InitializeComponent();

        AppPaths.EnsureCreated();
        _history = new ChangeHistory(AppPaths.HistoryFile);
        _backupManager = new BackupManager(AppPaths.BackupsFolder, _history);
        _restoreManager = new RestoreManager(_history);
        _env = WindowsEnvironment.Detect();

        LoadColorSchemes();
        LoadCompatibilityReport();
        LoadHistory();
        RefreshBackupList();
        RefreshStatusPanel();

        ChkRunOnStartup.IsChecked = StartupModule.IsEnabled();

        var lockSupport = LockScreenModule.DetectSupport(_env);
        TxtLockScreenInfo.Text = lockSupport switch
        {
            LockScreenSupport.PolicyBasedRequiresAdmin =>
                "Sua edição do Windows (Pro/Enterprise) permite aplicar a imagem da tela de bloqueio " +
                "via política de grupo. Isso exige privilégio de administrador uma única vez.",
            _ =>
                "O Windows Home não oferece uma forma oficial de trocar a tela de bloqueio por software. " +
                "Você pode trocar manualmente em Configurações > Personalização > Tela de bloqueio."
        };
        BtnLockScreen.IsEnabled = lockSupport == LockScreenSupport.PolicyBasedRequiresAdmin;
    }

    // ---------------- Painel principal ----------------

    private void RefreshStatusPanel()
    {
        RowDesktop.Text = $"DESKTOP              {(HasWallpaperApplied() ? "✓" : "—")}";
        RowColors.Text = "CORES                " + (HasSchemeApplied() ? "✓" : "—");
        RowCursors.Text = "CURSORES             " + (HasCursorsApplied() ? "✓" : "—");
        RowSounds.Text = "SONS                 " + (HasSoundsApplied() ? "✓" : "—");
        RowLockScreen.Text = "TELA DE BLOQUEIO     " +
            (LockScreenModule.DetectSupport(_env) == LockScreenSupport.Unsupported ? "✕" : "⚠");
        RowIcon.Text = "ÍCONE DO APLICATIVO  ✓";
        RowScreensaver.Text = "PROTEÇÃO DE TELA     " + (HasScreensaverApplied() ? "✓" : "—");
        RowOthers.Text = "OUTROS               ⚠";
        TxtCurrentTheme.Text = $"Tema atual: {_selectedScheme.Name}";
    }

    private static bool HasWallpaperApplied()
    {
        using var desktop = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
        var wallpaper = desktop?.GetValue("WallPaper") as string;
        return !string.IsNullOrEmpty(wallpaper) && wallpaper.Contains("TetrisWindowsEdition", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasSchemeApplied()
    {
        using var dwm = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
        return dwm?.GetValue("ColorPrevalence") is int i && i == 1;
    }

    private static bool HasCursorsApplied()
    {
        using var cursors = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors");
        return (cursors?.GetValue(string.Empty) as string) == "Tetris Windows Edition";
    }

    private static bool HasSoundsApplied()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"AppEvents\Schemes\Apps\.Default\SystemStart\.Current");
        var value = key?.GetValue(null) as string;
        return !string.IsNullOrEmpty(value) && value.Contains("tetris", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasScreensaverApplied()
    {
        using var desktop = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
        var scr = desktop?.GetValue("SCRNSAVE.EXE") as string;
        return !string.IsNullOrEmpty(scr) && scr.Contains("Tetris", StringComparison.OrdinalIgnoreCase);
    }

    private void BtnGoPersonalize_Click(object sender, RoutedEventArgs e) => TabPersonalize.IsSelected = true;
    private void BtnGoSettings_Click(object sender, RoutedEventArgs e) => TabSettings.IsSelected = true;

    private void BtnApplyTheme_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "Isso vai alterar papel de parede, cores, cursores, sons e proteção de tela.\n" +
            "Um backup automático do estado atual será criado antes.\n\nContinuar?",
            "Aplicar Tema Tetris", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            if (ChkAutoBackup.IsChecked == true)
                _backupManager.CreateBackup("antes_de_aplicar_tema");

            ColorsModule.Apply(_selectedScheme);
            _history.Log("Cores aplicadas", _selectedScheme.Name);

            var wallpaperPath = _customWallpaperPath ?? DefaultWallpaperForScheme(_selectedScheme);
            if (File.Exists(wallpaperPath))
            {
                WallpaperModule.SetWallpaperFile(wallpaperPath);
                _history.Log("Wallpaper aplicado", wallpaperPath);
            }

            CursorsModule.Apply(AppPaths.CursorsFolder);
            _history.Log("Cursores aplicados", "Esquema Tetris");

            SoundsModule.Apply(AppPaths.SoundsFolder);
            _history.Log("Sons aplicados", "Pacote Tetris");

            _history.Log("Tema Tetris aplicado", _selectedScheme.Name);
            LoadHistory();
            RefreshBackupList();
            RefreshStatusPanel();

            MessageBox.Show("LINHA COMPLETADA!\n\nTema Tetris aplicado com sucesso.", "Concluído",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível aplicar o tema completamente:\n{ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static string DefaultWallpaperForScheme(ColorScheme scheme) =>
        Path.Combine(AppPaths.WallpapersFolder, SafeFileName(scheme.Name) + ".png");

    private static string SafeFileName(string name) => name.Replace(" ", "_").ToLowerInvariant();

    private void BtnRestore_Click(object sender, RoutedEventArgs e)
    {
        var latest = _backupManager.ListBackups().FirstOrDefault();
        if (latest == null)
        {
            MessageBox.Show("Nenhum backup encontrado para restaurar.", "Restaurar Windows",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Isso vai restaurar o Windows para o estado salvo em:\n{Path.GetFileName(latest)}\n\nContinuar?",
            "Restaurar Windows", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        RestoreFromFile(latest);
    }

    private void BtnRestoreSelected_Click(object sender, RoutedEventArgs e)
    {
        if (ListBackups.SelectedItem is not string path)
        {
            MessageBox.Show("Selecione um backup na lista.", "Restaurar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        RestoreFromFile(path);
    }

    private void RestoreFromFile(string path)
    {
        try
        {
            var snapshot = _backupManager.LoadBackup(path);
            _restoreManager.Restore(snapshot);
            LoadHistory();
            RefreshStatusPanel();
            MessageBox.Show("Windows restaurado ao estado anterior.", "Concluído",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha ao restaurar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnBackup_Click(object sender, RoutedEventArgs e)
    {
        var path = _backupManager.CreateBackup("manual");
        RefreshBackupList();
        LoadHistory();
        MessageBox.Show($"Backup criado:\n{Path.GetFileName(path)}", "Backup", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnRefreshBackups_Click(object sender, RoutedEventArgs e) => RefreshBackupList();

    private void RefreshBackupList()
    {
        ListBackups.ItemsSource = null;
        ListBackups.ItemsSource = _backupManager.ListBackups().ToList();
    }

    // ---------------- Personalizar ----------------

    private void LoadColorSchemes()
    {
        CmbColorScheme.ItemsSource = ColorSchemes.All.Select(s => s.Name).ToList();
        CmbColorScheme.SelectedIndex = 0;
        CmbColorScheme.SelectionChanged += (_, _) =>
        {
            _selectedScheme = ColorSchemes.All[CmbColorScheme.SelectedIndex];
            RefreshStatusPanel();
        };
    }

    private void BtnChooseWallpaper_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Imagens (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp" };
        if (dialog.ShowDialog() == true)
        {
            _customWallpaperPath = dialog.FileName;
            TxtWallpaperPath.Text = dialog.FileName;
        }
    }

    private void BtnApplyCursors_Click(object sender, RoutedEventArgs e)
    {
        CursorsModule.Apply(AppPaths.CursorsFolder);
        _history.Log("Cursores aplicados", "Manual");
        LoadHistory();
        RefreshStatusPanel();
    }

    private void BtnRestoreCursors_Click(object sender, RoutedEventArgs e)
    {
        CursorsModule.RestoreWindowsDefault();
        _history.Log("Cursores restaurados", "Padrão do Windows");
        LoadHistory();
        RefreshStatusPanel();
    }

    private void BtnApplySounds_Click(object sender, RoutedEventArgs e)
    {
        SoundsModule.Apply(AppPaths.SoundsFolder);
        _history.Log("Sons aplicados", "Manual");
        LoadHistory();
        RefreshStatusPanel();
    }

    private void BtnSilenceSounds_Click(object sender, RoutedEventArgs e)
    {
        SoundsModule.RestoreSilence();
        _history.Log("Sons silenciados", "Manual");
        LoadHistory();
        RefreshStatusPanel();
    }

    private void BtnEnableScreensaver_Click(object sender, RoutedEventArgs e)
    {
        var scrPath = Path.Combine(AppContext.BaseDirectory, "TetrisWindowsEdition.Screensaver.scr");
        if (!File.Exists(scrPath))
        {
            MessageBox.Show(
                "O arquivo da proteção de tela (.scr) não foi encontrado ao lado do app.\n" +
                "Compile o projeto TetrisWindowsEdition.Screensaver e copie o .exe renomeado para .scr " +
                "para esta mesma pasta.", "Proteção de tela", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ScreensaverModule.Register(scrPath);
        _history.Log("Proteção de tela ativada", "Tetris");
        LoadHistory();
        RefreshStatusPanel();
    }

    private void BtnDisableScreensaver_Click(object sender, RoutedEventArgs e)
    {
        ScreensaverModule.Unregister();
        _history.Log("Proteção de tela desativada", string.Empty);
        LoadHistory();
        RefreshStatusPanel();
    }

    private void BtnLockScreen_Click(object sender, RoutedEventArgs e)
    {
        var wallpaperPath = _customWallpaperPath ?? DefaultWallpaperForScheme(_selectedScheme);
        if (!File.Exists(wallpaperPath))
        {
            MessageBox.Show("Escolha ou gere uma imagem de wallpaper antes.", "Tela de bloqueio",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var ok = LockScreenModule.ApplyViaPolicy(wallpaperPath);
        _history.Log("Tela de bloqueio", ok ? "Aplicada via política de grupo" : "Cancelada pelo usuário (UAC)");
        LoadHistory();
        RefreshStatusPanel();
    }

    private void BtnExportTheme_Click(object sender, RoutedEventArgs e)
    {
        var wallpaperPath = _customWallpaperPath ?? DefaultWallpaperForScheme(_selectedScheme);
        var exported = ThemeExporter.Export(AppPaths.ExportedThemesFolder, _selectedScheme.Name, _selectedScheme, wallpaperPath);
        _history.Log("Tema exportado", exported);
        LoadHistory();
        MessageBox.Show($"Tema exportado para:\n{exported}", "Exportar tema", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ---------------- Configurações ----------------

    private void ChkRunOnStartup_Changed(object sender, RoutedEventArgs e)
    {
        if (ChkRunOnStartup.IsChecked == true)
        {
            StartupModule.Enable();
            _history.Log("Inicialização automática", "Ativada");
        }
        else
        {
            StartupModule.Disable();
            _history.Log("Inicialização automática", "Desativada");
        }
        LoadHistory();
    }

    // ---------------- Compatibilidade / Histórico ----------------

    private void LoadCompatibilityReport()
    {
        var symbol = new Func<SupportLevel, string>(level => level switch
        {
            SupportLevel.FullySupported => "✓",
            SupportLevel.Partial => "⚠",
            _ => "✕"
        });

        ListCompatibility.ItemsSource = CompatibilityAnalyzer.Analyze(_env)
            .Select(i => $"{symbol(i.Level)}  {i.Feature} — {i.Explanation}")
            .ToList();
    }

    private void LoadHistory()
    {
        ListHistory.ItemsSource = _history.Entries
            .OrderByDescending(h => h.TimestampUtc)
            .Select(h => $"{h.TimestampUtc.ToLocalTime():dd/MM/yyyy HH:mm} — {h.Action}" +
                         (string.IsNullOrEmpty(h.Details) ? "" : $" ({h.Details})"))
            .ToList();
    }
}
