using WinClear.Models;
using WinClear.ScannerProviders;

namespace WinClear.Services;

public class ScanEngine
{
    private readonly List<IScanner> _scanners;
    public ExclusionManager ExclusionManager { get; } = new();
    public CleanupHistoryService CleanupHistory { get; } = new();

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
            new PrivacyTracesScanner(),
            new SystemSlimScanner(),
        };
    }

    public UserDefinedScanner UserDefinedScanner { get; } = new();

    public async Task<ScanResult> RunScanAsync(ScanTarget scanTarget, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var result = new ScanResult();
        var activeScanners = _scanners
            .Where(s => scanTarget.ScannerEnabled.GetValueOrDefault(s.CategoryName, true))
            .ToList();

        for (int i = 0; i < activeScanners.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scanner = activeScanners[i];

            if (scanner is LargeFileScanner or DuplicateFileScanner)
                scanner.TargetPaths = scanTarget.SelectedDrives;

            scanner.TargetPaths ??= new List<string>();

            var items = await scanner.ScanAsync(
                progress == null ? null : new Progress<double>(p =>
                    progress.Report((i + p) / (activeScanners.Count + (scanTarget.CustomPaths.Count > 0 ? 1 : 0)))),
                cancellationToken);

            var filteredItems = items
                .Where(f => !ExclusionManager.IsExcluded(f.FullPath))
                .ToList();

            if (filteredItems.Count > 0)
            {
                var categoryNode = new FileItem
                {
                    Name = scanner.CategoryName,
                    FullPath = scanner.CategoryName,
                    IsSelected = true,
                    Category = scanner.CategoryName,
                    SourceApp = scanner.SourceApp,
                    SizeBytes = filteredItems.Sum(f => f.SizeBytes),
                    SafetyTag = SafetyTag.Safe,
                };
                categoryNode.Children = new System.Collections.ObjectModel.ObservableCollection<FileItem>(filteredItems);
                result.Categories.Add(categoryNode);
                result.TotalSize += categoryNode.SizeBytes;
                result.TotalFiles += filteredItems.Count;
            }
        }

        if (scanTarget.CustomPaths.Count > 0)
        {
            UserDefinedScanner.CustomPaths = scanTarget.CustomPaths;
            var udItems = await UserDefinedScanner.ScanAsync(progress, cancellationToken);
            var filteredUd = udItems.Where(f => !ExclusionManager.IsExcluded(f.FullPath)).ToList();
            if (filteredUd.Count > 0)
            {
                var categoryNode = new FileItem
                {
                    Name = UserDefinedScanner.CategoryName,
                    FullPath = UserDefinedScanner.CategoryName,
                    IsSelected = true,
                    Category = UserDefinedScanner.CategoryName,
                    SourceApp = UserDefinedScanner.SourceApp,
                    SizeBytes = filteredUd.Sum(f => f.SizeBytes),
                    SafetyTag = SafetyTag.Safe,
                };
                categoryNode.Children = new System.Collections.ObjectModel.ObservableCollection<FileItem>(filteredUd);
                result.Categories.Add(categoryNode);
                result.TotalSize += categoryNode.SizeBytes;
                result.TotalFiles += filteredUd.Count;
            }
        }

        return result;
    }
}
