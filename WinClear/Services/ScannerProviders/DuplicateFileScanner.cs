using System.IO;
using System.Security.Cryptography;
using WinClear.Models;
using WinClear.Services;

namespace WinClear.ScannerProviders;

public class DuplicateFileScanner : IScanner
{
    public string CategoryName => "重复文件";
    public string SourceApp => "重复文件";
    public List<string>? TargetPaths { get; set; }

    public async Task<List<FileItem>> ScanAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var allFiles = new List<FileInfo>();
        var drives = TargetPaths?.Count > 0
            ? TargetPaths
            : DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
                .Select(d => d.RootDirectory.FullName);

        foreach (var drive in drives)
        {
            try
            {
                var topDirs = Directory.GetDirectories(drive);
                foreach (var dir in topDirs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        CollectFiles(dir, allFiles, cancellationToken);
                    }
                    catch { }
                }
            }
            catch { }
        }

        var sizeGroups = allFiles
            .GroupBy(f => f.Length)
            .Where(g => g.Count() > 1)
            .ToList();

        var items = new List<FileItem>();
        for (int i = 0; i < sizeGroups.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report((double)i / sizeGroups.Count);

            var hashGroups = new Dictionary<string, List<FileInfo>>();
            foreach (var file in sizeGroups[i])
            {
                var hash = await ComputeMd5Async(file.FullName);
                if (hash == null) continue;

                if (!hashGroups.ContainsKey(hash))
                    hashGroups[hash] = new List<FileInfo>();
                hashGroups[hash].Add(file);
            }

            foreach (var group in hashGroups.Values.Where(g => g.Count > 1))
            {
                bool isFirst = true;
                foreach (var file in group)
                {
                    items.Add(new FileItem
                    {
                        Name = file.Name,
                        FullPath = file.FullName,
                        SizeBytes = file.Length,
                        Category = CategoryName,
                        SourceApp = SourceApp,
                        SafetyTag = SafetyTag.Warning,
                        IsSelected = !isFirst
                    });
                    isFirst = false;
                }
            }
        }

        return items;
    }

    private void CollectFiles(string dirPath, List<FileInfo> files, CancellationToken ct)
    {
        try
        {
            foreach (var file in Directory.GetFiles(dirPath))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    files.Add(new FileInfo(file));
                }
                catch { }
            }

            foreach (var subDir in Directory.GetDirectories(dirPath))
            {
                ct.ThrowIfCancellationRequested();
                CollectFiles(subDir, files, ct);
            }
        }
        catch { }
    }

    private static async Task<string?> ComputeMd5Async(string filePath)
    {
        try
        {
            await using var stream = File.OpenRead(filePath);
            var hash = await MD5.Create().ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
        catch
        {
            return null;
        }
    }
}
