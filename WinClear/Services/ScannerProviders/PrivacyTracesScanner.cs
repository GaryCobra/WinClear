using System.IO;
using WinClear.Models;
using WinClear.Services;

namespace WinClear.ScannerProviders;

public class PrivacyTracesScanner : IScanner
{
    public string CategoryName => "隐私痕迹";
    public string SourceApp => "系统";
    public List<string>? TargetPaths { get; set; }

    private static readonly string[] _targetDirs =
    {
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Windows\Recent"),
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Windows\Recent\AutomaticDestinations"),
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Windows\Recent\CustomDestinations"),
    };

    public async Task<List<FileItem>> ScanAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var items = new List<FileItem>();
        for (int i = 0; i < _targetDirs.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report((double)i / _targetDirs.Length);

            var dir = _targetDirs[i];
            if (!Directory.Exists(dir)) continue;

            try
            {
                var files = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
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
