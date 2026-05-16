using WinClear.Models;
using WinClear.ScannerProviders;

namespace WinClear.Services;

public class ScanEngine
{
    private readonly List<IScanner> _scanners;

    public ScanEngine()
    {
        _scanners = new List<IScanner>
        {
            new SystemTempScanner(),
            new WindowsUpdateScanner(),
            new BrowserCacheScanner(),
            new AppCacheScanner(),
            new LargeFileScanner(),
            new DuplicateFileScanner(),
        };
    }

    public UserDefinedScanner UserDefinedScanner { get; } = new();

    public async Task<ScanResult> RunScanAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var result = new ScanResult();
        var allResults = new List<List<FileItem>>();

        for (int i = 0; i < _scanners.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scanner = _scanners[i];
            var items = await scanner.ScanAsync(
                progress == null ? null : new Progress<double>(p =>
                    progress.Report((i + p) / (_scanners.Count + (UserDefinedScanner.CustomPaths.Count > 0 ? 1 : 0)))),
                cancellationToken);

            if (items.Count > 0)
            {
                var categoryNode = new FileItem
                {
                    Name = scanner.CategoryName,
                    FullPath = scanner.CategoryName,
                    IsSelected = true,
                    Category = scanner.CategoryName,
                    SourceApp = scanner.SourceApp,
                    SizeBytes = items.Sum(f => f.SizeBytes),
                    SafetyTag = SafetyTag.Safe,
                };
                categoryNode.Children = new System.Collections.ObjectModel.ObservableCollection<FileItem>(items);
                result.Categories.Add(categoryNode);
                result.TotalSize += categoryNode.SizeBytes;
                result.TotalFiles += items.Count;
            }
        }

        if (UserDefinedScanner.CustomPaths.Count > 0)
        {
            var udItems = await UserDefinedScanner.ScanAsync(progress, cancellationToken);
            if (udItems.Count > 0)
            {
                var categoryNode = new FileItem
                {
                    Name = UserDefinedScanner.CategoryName,
                    FullPath = UserDefinedScanner.CategoryName,
                    IsSelected = true,
                    Category = UserDefinedScanner.CategoryName,
                    SourceApp = UserDefinedScanner.SourceApp,
                    SizeBytes = udItems.Sum(f => f.SizeBytes),
                    SafetyTag = SafetyTag.Safe,
                };
                categoryNode.Children = new System.Collections.ObjectModel.ObservableCollection<FileItem>(udItems);
                result.Categories.Add(categoryNode);
                result.TotalSize += categoryNode.SizeBytes;
                result.TotalFiles += udItems.Count;
            }
        }

        return result;
    }
}
