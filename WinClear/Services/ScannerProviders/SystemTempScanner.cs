using System.IO;
using WinClear.Models;
using WinClear.Services;

namespace WinClear.ScannerProviders;

public class SystemTempScanner : IScanner
{
    public string CategoryName => "系统临时文件";
    public string SourceApp => "系统";
    public List<string>? TargetPaths { get; set; }

    public async Task<List<FileItem>> ScanAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var items = new List<FileItem>();
        var paths = new[]
        {
            Path.GetTempPath(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
        };

        int totalDirs = paths.Length;
        for (int i = 0; i < totalDirs; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report((double)i / totalDirs);

            var dir = paths[i];
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
