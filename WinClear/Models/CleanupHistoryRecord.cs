namespace WinClear.Models;

public class CleanupHistoryRecord
{
    public DateTime Timestamp { get; set; }
    public int FilesDeleted { get; set; }
    public long SizeFreed { get; set; }
    public List<string> ScannerSources { get; set; } = new();
}
