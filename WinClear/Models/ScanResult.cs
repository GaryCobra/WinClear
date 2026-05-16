using System.Collections.ObjectModel;

namespace WinClear.Models;

public class ScanResult
{
    public ObservableCollection<FileItem> Categories { get; set; } = new();
    public long TotalSize { get; set; }
    public int TotalFiles { get; set; }
}
