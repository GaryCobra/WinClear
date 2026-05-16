using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinClear.Models;

public partial class FileItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _fullPath = string.Empty;

    [ObservableProperty]
    private long _sizeBytes;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private string _sourceApp = string.Empty;

    [ObservableProperty]
    private SafetyTag _safetyTag;

    [ObservableProperty]
    private bool _isSelected;

    public string SizeFormatted => Helpers.FileSizeFormatter.Format(SizeBytes);

    public ObservableCollection<FileItem> Children { get; set; } = new();
}
