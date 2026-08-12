using System.Text.Json;

namespace TetrisWindowsEdition.Modules;

public sealed record HistoryEntry(DateTime TimestampUtc, string Action, string Details);

/// <summary>
/// Item 18 do spec: histórico de alterações, para permitir desfazer
/// individualmente e para transparência total do que o app já mexeu.
/// </summary>
public sealed class ChangeHistory
{
    private readonly string _filePath;
    private readonly List<HistoryEntry> _entries = new();

    public IReadOnlyList<HistoryEntry> Entries => _entries;

    public ChangeHistory(string filePath)
    {
        _filePath = filePath;
        Load();
    }

    public void Log(string action, string details)
    {
        _entries.Add(new HistoryEntry(DateTime.UtcNow, action, details));
        Save();
    }

    private void Load()
    {
        if (!File.Exists(_filePath)) return;
        try
        {
            var json = File.ReadAllText(_filePath);
            var loaded = JsonSerializer.Deserialize<List<HistoryEntry>>(json);
            if (loaded != null)
            {
                _entries.Clear();
                _entries.AddRange(loaded);
            }
        }
        catch
        {
            // Histórico corrompido não deve impedir o app de abrir.
            // Preferimos começar um histórico novo a travar a aplicação.
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
