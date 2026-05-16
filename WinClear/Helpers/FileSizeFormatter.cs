namespace WinClear.Helpers;

public static class FileSizeFormatter
{
    private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };

    public static string Format(long bytes)
    {
        if (bytes == 0) return "0 B";

        int unitIndex = 0;
        double size = bytes;

        while (size >= 1024 && unitIndex < Units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return unitIndex switch
        {
            0 => $"{size:F0} {Units[unitIndex]}",
            _ => $"{size:F1} {Units[unitIndex]}"
        };
    }
}
