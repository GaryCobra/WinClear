using WinClear.Models;

namespace WinClear.Services;

public interface IScanner
{
    string CategoryName { get; }
    string SourceApp { get; }
    List<string>? TargetPaths { get; set; }
    Task<List<FileItem>> ScanAsync(IProgress<double>? progress, CancellationToken cancellationToken);
}
