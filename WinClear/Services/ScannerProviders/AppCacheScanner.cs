using System.IO;
using WinClear.Models;
using WinClear.Services;

namespace WinClear.ScannerProviders;

public class AppCacheScanner : IScanner
{
    public string CategoryName => "应用缓存";
    public string SourceApp => "应用";
    public List<string>? TargetPaths { get; set; }

    private static readonly (string App, string Path)[] _appPaths =
    {
        ("微信", Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Tencent\WeChat")),
        ("QQ", Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Tencent\QQ")),
    };

    public async Task<List<FileItem>> ScanAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var items = new List<FileItem>();

        for (int i = 0; i < _appPaths.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report((double)i / _appPaths.Length);

            var (app, path) = _appPaths[i];
            if (!Directory.Exists(path)) continue;

            try
            {
                var customDataDirs = Directory.GetDirectories(path, "*WeChat Files*", SearchOption.TopDirectoryOnly);
                foreach (var dataDir in customDataDirs)
                {
                    var cacheDirs = new[]
                    {
                        Path.Combine(dataDir, "FileStorage", "Image"),
                        Path.Combine(dataDir, "FileStorage", "Video"),
                        Path.Combine(dataDir, "FileStorage", "File"),
                    };

                    foreach (var cacheDir in cacheDirs)
                    {
                        if (!Directory.Exists(cacheDir)) continue;
                        try
                        {
                            var files = Directory.GetFiles(cacheDir, "*", SearchOption.AllDirectories);
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
                                        Category = $"{CategoryName} - {app}",
                                        SourceApp = app,
                                        SafetyTag = SafetyTag.Warning
                                    });
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        return items;
    }
}
