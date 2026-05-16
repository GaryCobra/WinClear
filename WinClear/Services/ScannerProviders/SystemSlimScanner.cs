using System.IO;
using WinClear.Models;
using WinClear.Services;

namespace WinClear.ScannerProviders;

public class SystemSlimScanner : IScanner
{
    public string CategoryName => "系统瘦身";
    public string SourceApp => "Windows";
    public List<string>? TargetPaths { get; set; }

    public async Task<List<FileItem>> ScanAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var items = new List<FileItem>();
        var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var targets = new List<(string DirPath, SafetyTag Tag)>
        {
            (System.IO.Path.Combine(winDir, "Help"), SafetyTag.Warning),
            (System.IO.Path.Combine(winDir, @"System32\oobe\info\backgrounds"), SafetyTag.Safe),
            (System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Web", "Wallpaper"), SafetyTag.Warning),
        };

        var oldWinDir = System.IO.Path.Combine(
            System.IO.Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? "C:\\", "Windows.old");
        if (Directory.Exists(oldWinDir))
            targets.Add((oldWinDir, SafetyTag.Danger));

        for (int i = 0; i < targets.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report((double)i / targets.Count);

            var (dir, tag) = targets[i];
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
                            SafetyTag = tag
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
