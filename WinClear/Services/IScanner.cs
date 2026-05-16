using WinClear.Models;

namespace WinClear.Services;

public interface IScanner
{
    string CategoryName { get; }
    string SourceApp { get; }
    Task<List<FileItem>> ScanAsync(IProgress<double>? progress, CancellationToken cancellationToken);
}
