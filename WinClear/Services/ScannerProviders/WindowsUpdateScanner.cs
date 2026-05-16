using System.IO;
using WinClear.Models;
using WinClear.Services;

namespace WinClear.ScannerProviders;

public class WindowsUpdateScanner : IScanner
{
    public string CategoryName => "Windows 更新缓存";
    public string SourceApp => "Windows 更新";

    public async Task<List<FileItem>> ScanAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var items = new List<FileItem>();
        var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var paths = new[]
        {
            Path.Combine(winDir, "SoftwareDistribution", "Download"),
        };

        foreach (var dir in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(dir)) continue;

            try
            {
                var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        var info = new FileInfo(file);
                        items.Add(new FileItem
                        {
                            Name = info.Name,
                            FullPath = info.FullName,
                            SizeBytes = info.Length,
                            Category = CategoryName,
                            SourceApp = SourceApp,
                            SafetyTag = SafetyTag.Safe
                        });
                    }
                    catch { }
                }
            }
            catch { }
        }

        return items;
    }
}
