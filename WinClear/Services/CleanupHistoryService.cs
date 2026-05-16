using System.IO;
using System.Text.Json;
using WinClear.Models;

namespace WinClear.Services;

public class CleanupHistoryService
{
    private List<CleanupHistoryRecord> _records = new();
    private readonly string _filePath;
    private const int MaxRecords = 100;

    public CleanupHistoryService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinClear");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "history.json");
        Load();
    }

    public long TotalFreedBytes => _records.Sum(r => r.SizeFreed);
    public int TotalFilesDeleted => _records.Sum(r => r.FilesDeleted);
    public IReadOnlyList<CleanupHistoryRecord> Records => _records.AsReadOnly();

    public void Record(int filesDeleted, long sizeFreed, List<string> scannerSources)
    {
        _records.Add(new CleanupHistoryRecord
        {
            Timestamp = DateTime.Now,
            FilesDeleted = filesDeleted,
            SizeFreed = sizeFreed,
            ScannerSources = scannerSources
        });

        if (_records.Count > MaxRecords)
            _records = _records.TakeLast(MaxRecords).ToList();

        Save();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _records = JsonSerializer.Deserialize<List<CleanupHistoryRecord>>(json) ?? new();
            }
        }
        catch { _records = new(); }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_records, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch { }
    }
}
