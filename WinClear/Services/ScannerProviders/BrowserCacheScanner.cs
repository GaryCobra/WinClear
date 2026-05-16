using System.IO;
using WinClear.Models;
using WinClear.Services;

namespace WinClear.ScannerProviders;

public class BrowserCacheScanner : IScanner
{
    public string CategoryName => "浏览器缓存";
    public string SourceApp => "浏览器";

    private static readonly (string Name, string Path)[] _browserPaths =
    {
        ("Chrome", Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Google\Chrome\User Data\Default\Cache")),
        ("Chrome Code", Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Google\Chrome\User Data\Default\Code Cache")),
        ("Edge", Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Microsoft\Edge\User Data\Default\Cache")),
    };

    public async Task<List<FileItem>> ScanAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var items = new List<FileItem>();

        for (int i = 0; i < _browserPaths.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report((double)i / _browserPaths.Length);

            var (name, path) = _browserPaths[i];
            if (!Directory.Exists(path)) continue;

            try
            {
                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
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
                            Category = $"{CategoryName} - {name}",
                            SourceApp = name,
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
