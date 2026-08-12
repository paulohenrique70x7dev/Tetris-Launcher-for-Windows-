using System.Text.Json;
using System.Windows.Forms;

namespace TetrisWindowsEdition.Screensaver;

/// <summary>
/// Caixa de configuração aberta quando o Windows chama "TetrisWindowsEdition.Screensaver.scr /c".
/// Só mexe no arquivo de config próprio do app — nunca em nada do sistema.
/// </summary>
public sealed class ConfigForm : Form
{
    private readonly TrackBar _speed = new() { Minimum = 1, Maximum = 10 };
    private readonly TrackBar _pieceCount = new() { Minimum = 4, Maximum = 40 };
    private readonly TrackBar _size = new() { Minimum = 12, Maximum = 48 };
    private readonly ComboBox _colorMode = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _sounds = new() { Text = "Sons ativados" };

    public ConfigForm()
    {
        Text = "Tetris Windows Edition — Proteção de tela";
        Width = 360;
        Height = 320;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;

        var config = ScreensaverConfig.Load();
        _speed.Value = Math.Clamp(config.Speed, _speed.Minimum, _speed.Maximum);
        _pieceCount.Value = Math.Clamp(config.PieceCount, _pieceCount.Minimum, _pieceCount.Maximum);
        _size.Value = Math.Clamp(config.Size, _size.Minimum, _size.Maximum);
        _colorMode.Items.AddRange(new object[] { "classic", "neon", "monochrome" });
        _colorMode.SelectedItem = config.ColorMode;
        _sounds.Checked = config.SoundsEnabled;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(12) };
        layout.Controls.Add(new Label { Text = "Velocidade" });
        layout.Controls.Add(_speed);
        layout.Controls.Add(new Label { Text = "Quantidade de peças" });
        layout.Controls.Add(_pieceCount);
        layout.Controls.Add(new Label { Text = "Tamanho" });
        layout.Controls.Add(_size);
        layout.Controls.Add(new Label { Text = "Modo de cores" });
        layout.Controls.Add(_colorMode);
        layout.Controls.Add(_sounds);

        var btnSave = new Button { Text = "Salvar", DialogResult = DialogResult.OK };
        btnSave.Click += (_, _) => Save();
        layout.Controls.Add(btnSave);

        Controls.Add(layout);
    }

    private void Save()
    {
        var config = new ScreensaverConfig
        {
            Speed = _speed.Value,
            PieceCount = _pieceCount.Value,
            Size = _size.Value,
            ColorMode = _colorMode.SelectedItem as string ?? "classic",
            SoundsEnabled = _sounds.Checked
        };

        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TetrisWindowsEdition", "screensaver_config.json");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        Close();
    }
}
