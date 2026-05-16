using System.IO;
using WinClear.Models;

namespace WinClear.Services;

public static class SafetyClassifier
{
    private static readonly HashSet<string> _dangerExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".sys", ".com", ".ocx", ".scr", ".drv"
    };

    public static SafetyTag Classify(string filePath, string sourceApp, long sizeBytes)
    {
        var extension = Path.GetExtension(filePath);

        if (_dangerExtensions.Contains(extension))
            return SafetyTag.Danger;

        var fileName = Path.GetFileName(filePath).ToLowerInvariant();

        if (fileName is "thumbs.db" or "desktop.ini")
            return SafetyTag.Safe;

        if (filePath.Contains("$Recycle.Bin", StringComparison.OrdinalIgnoreCase))
            return SafetyTag.Safe;

        if (sourceApp.Contains("更新", StringComparison.OrdinalIgnoreCase) &&
            (extension is ".bak" or ".log" or ".pdb"))
            return SafetyTag.Safe;

        if (sourceApp.Contains("大文件", StringComparison.OrdinalIgnoreCase) ||
            sourceApp.Contains("重复文件", StringComparison.OrdinalIgnoreCase))
            return SafetyTag.Warning;

        return SafetyTag.Safe;
    }
}
