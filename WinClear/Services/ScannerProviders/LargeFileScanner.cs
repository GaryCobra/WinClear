using System.IO;
using WinClear.Models;
using WinClear.Services;

namespace WinClear.ScannerProviders;

public class LargeFileScanner : IScanner
{
    public string CategoryName => "大文件";
    public string SourceApp => "大文件";

    public long MinSizeBytes { get; set; } = 100L * 1024 * 1024;

    public async Task<List<FileItem>> ScanAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var items = new List<FileItem>();
        var drives = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
            .Select(d => d.RootDirectory.FullName);

        foreach (var drive in drives)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var topDirs = Directory.GetDirectories(drive);
                for (int i = 0; i < topDirs.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.Report((double)i / topDirs.Length);

                    try
                    {
                        WalkDirectory(topDirs[i], items, cancellationToken);
                    }
                    catch { }
                }
            }
            catch { }
        }

        return items.OrderByDescending(f => f.SizeBytes).Take(500).ToList();
    }

    private void WalkDirectory(string dirPath, List<FileItem> items, CancellationToken ct)
    {
        try
        {
            foreach (var file in Directory.GetFiles(dirPath))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var info = new FileInfo(file);
                    if (info.Length >= MinSizeBytes)
                    {
                        items.Add(new FileItem
                        {
                            Name = info.Name,
                            FullPath = info.FullName,
                            SizeBytes = info.Length,
                            Category = CategoryName,
                            SourceApp = SourceApp,
                            SafetyTag = SafetyTag.Warning
                        });
                    }
                }
                catch { }
            }

            foreach (var subDir in Directory.GetDirectories(dirPath))
            {
                ct.ThrowIfCancellationRequested();
                WalkDirectory(subDir, items, ct);
            }
        }
        catch { }
    }
}
