using System.IO;
using WinClear.Models;
using WinClear.Services;

namespace WinClear.ScannerProviders;

public class UserDefinedScanner : IScanner
{
    public string CategoryName => "自定义路径";
    public string SourceApp => "用户自定义";

    public List<string> CustomPaths { get; set; } = new();

    public async Task<List<FileItem>> ScanAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var items = new List<FileItem>();

        for (int i = 0; i < CustomPaths.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report((double)i / CustomPaths.Count);

            var path = CustomPaths[i];
            if (!Directory.Exists(path) && !File.Exists(path)) continue;

            try
            {
                if (File.Exists(path))
                {
                    var info = new FileInfo(path);
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
                else
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
                                Category = CategoryName,
                                SourceApp = SourceApp,
                                SafetyTag = SafetyTag.Safe
                            });
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
