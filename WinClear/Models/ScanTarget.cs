using System.IO;

namespace WinClear.Models;

public class ScanTarget
{
    public List<string> SelectedDrives { get; set; } = new();
    public List<string> CustomPaths { get; set; } = new();
    public Dictionary<string, bool> ScannerEnabled { get; set; } = new();

    public static ScanTarget CreateDefault()
    {
        var drives = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
            .Select(d => d.RootDirectory.FullName)
            .ToList();

        return new ScanTarget
        {
            SelectedDrives = drives,
            CustomPaths = new List<string>(),
            ScannerEnabled = new Dictionary<string, bool>
            {
                ["系统临时文件"] = true,
                ["浏览器缓存"] = true,
                ["应用缓存"] = true,
                ["Windows 更新缓存"] = true,
                ["大文件"] = true,
                ["重复文件"] = true,
                ["隐私痕迹"] = true,
                ["系统瘦身"] = true,
            }
        };
    }
}
