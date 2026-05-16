using System.IO;
using System.Text.Json;
using WinClear.Models;

namespace WinClear.Services;

public class ExclusionManager
{
    private List<ExclusionEntry> _exclusions = new();
    private readonly string _filePath;

    public ExclusionManager()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinClear");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "exclusions.json");
        Load();
    }

    public IReadOnlyList<ExclusionEntry> Exclusions => _exclusions.AsReadOnly();

    public void Add(string pattern, string description = "")
    {
        _exclusions.Add(new ExclusionEntry { Pattern = pattern, Description = description });
        Save();
    }

    public void Remove(ExclusionEntry entry)
    {
        _exclusions.Remove(entry);
        Save();
    }

    public bool IsExcluded(string filePath)
    {
        return _exclusions.Any(e =>
            filePath.StartsWith(e.Pattern, StringComparison.OrdinalIgnoreCase));
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _exclusions = JsonSerializer.Deserialize<List<ExclusionEntry>>(json) ?? new();
            }
        }
        catch { _exclusions = new(); }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_exclusions, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch { }
    }
}
