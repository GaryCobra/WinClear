# WinClear 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建一个 Windows 10/11 系统垃圾清理桌面应用，支持扫描并分类展示垃圾文件、大文件和重复文件，用户可勾选删除。

**架构:** C# WPF (.NET 8) 单体应用，MVVM 架构。扫描引擎采用可插拔的 IScanner 接口设计，每种垃圾类型对应独立 Scanner 实现。SafetyClassifier 基于规则表对文件进行安全等级分类。

**Tech Stack:** C# .NET 8, WPF, CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection, Serilog

**前置条件:** 确保已安装 .NET 8 SDK（可通过 `winget install Microsoft.DotNet.SDK.8` 安装）

---

### Task 1: 项目脚手架搭建

**Files:**
- Create: `WinClear/WinClear.sln`
- Create: `WinClear/WinClear/WinClear.csproj`
- Create: `WinClear/WinClear/App.xaml`
- Create: `WinClear/WinClear/App.xaml.cs`

- [ ] **Step 1: 安装 .NET 8 SDK（如未安装）**

检查 SDK 是否可用：

```
dotnet --list-sdks
```

如果没有 8.0.x，运行：

```
winget install Microsoft.DotNet.SDK.8
```

- [ ] **Step 2: 创建项目**

```bash
cd D:\workspace\WinClear
dotnet new sln -n WinClear
dotnet new wpf -n WinClear -o WinClear
dotnet sln add WinClear/WinClear.csproj
```

- [ ] **Step 3: 添加 NuGet 包**

```bash
cd D:\workspace\WinClear\WinClear
dotnet add package CommunityToolkit.Mvvm --version 8.2.2
dotnet add package Microsoft.Extensions.DependencyInjection --version 8.0.0
dotnet add package Serilog --version 3.1.1
dotnet add package Serilog.Sinks.File --version 5.0.0
dotnet add package Serilog.Sinks.Debug --version 2.0.0
```

- [ ] **Step 4: 创建目录结构**

```bash
mkdir WinClear\Models
mkdir WinClear\ViewModels
mkdir WinClear\Services\ScannerProviders
mkdir WinClear\Helpers
mkdir WinClear\Converters
```

- [ ] **Step 5: 验证构建通过**

```bash
cd D:\workspace\WinClear
dotnet build
```

Expected: `Build succeeded.`

---

### Task 2: 数据模型层

**Files:**
- Create: `WinClear/WinClear/Models/SafetyTag.cs`
- Create: `WinClear/WinClear/Models/FileItem.cs`
- Create: `WinClear/WinClear/Models/ScanResult.cs`

- [ ] **Step 1: 创建 SafetyTag 枚举**

`WinClear/WinClear/Models/SafetyTag.cs`:

```csharp
namespace WinClear.Models;

public enum SafetyTag
{
    Safe,
    Warning,
    Danger
}
```

- [ ] **Step 2: 创建 FileItem 模型**

`WinClear/WinClear/Models/FileItem.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinClear.Models;

public partial class FileItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _fullPath = string.Empty;

    [ObservableProperty]
    private long _sizeBytes;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private string _sourceApp = string.Empty;

    [ObservableProperty]
    private SafetyTag _safetyTag;

    [ObservableProperty]
    private bool _isSelected;

    public string SizeFormatted => FileSizeFormatter.Format(SizeBytes);

    public ObservableCollection<FileItem> Children { get; set; } = new();
}
```

- [ ] **Step 3: 创建 ScanResult 模型**

`WinClear/WinClear/Models/ScanResult.cs`:

```csharp
using System.Collections.ObjectModel;

namespace WinClear.Models;

public class ScanResult
{
    public ObservableCollection<FileItem> Categories { get; set; } = new();
    public long TotalSize { get; set; }
    public int TotalFiles { get; set; }
}
```

- [ ] **Step 4: 验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

---

### Task 3: 核心服务层

**Files:**
- Create: `WinClear/WinClear/Services/IScanner.cs`
- Create: `WinClear/WinClear/Services/SafetyClassifier.cs`
- Create: `WinClear/WinClear/Helpers/FileSizeFormatter.cs`

- [ ] **Step 1: 创建 IScanner 接口**

`WinClear/WinClear/Services/IScanner.cs`:

```csharp
using WinClear.Models;

namespace WinClear.Services;

public interface IScanner
{
    string CategoryName { get; }
    string SourceApp { get; }
    Task<List<FileItem>> ScanAsync(IProgress<double>? progress, CancellationToken cancellationToken);
}
```

- [ ] **Step 2: 创建 FileSizeFormatter**

`WinClear/WinClear/Helpers/FileSizeFormatter.cs`:

```csharp
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
```

- [ ] **Step 3: 初步实现 SafetyClassifier**

`WinClear/WinClear/Services/SafetyClassifier.cs`:

```csharp
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
```

- [ ] **Step 4: 验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

---

### Task 4: 系统级扫描器

**Files:**
- Create: `WinClear/WinClear/Services/ScannerProviders/SystemTempScanner.cs`
- Create: `WinClear/WinClear/Services/ScannerProviders/WindowsUpdateScanner.cs`

- [ ] **Step 1: 创建 SystemTempScanner**

`WinClear/WinClear/Services/ScannerProviders/SystemTempScanner.cs`:

```csharp
using WinClear.Models;
using WinClear.Services;

namespace WinClear.ScannerProviders;

public class SystemTempScanner : IScanner
{
    public string CategoryName => "系统临时文件";
    public string SourceApp => "系统";

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
```

- [ ] **Step 2: 创建 WindowsUpdateScanner**

`WinClear/WinClear/Services/ScannerProviders/WindowsUpdateScanner.cs`:

```csharp
using WinClear.Models;
using WinClear.Services;

namespace WinClear.ScannerProviders;

public class WindowsUpdateScanner : IScanner
{
    public string CategoryName => "Windows 更新缓存";
    public string SourceApp => "Windows 更新";

    public async Task<List<FileItem>> ScanAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var items = new List<FileItem>();
        var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var paths = new[]
        {
            Path.Combine(winDir, "SoftwareDistribution", "Download"),
        };

        foreach (var dir in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
```

- [ ] **Step 3: 验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

---

### Task 5: 应用缓存扫描器

**Files:**
- Create: `WinClear/WinClear/Services/ScannerProviders/BrowserCacheScanner.cs`
- Create: `WinClear/WinClear/Services/ScannerProviders/AppCacheScanner.cs`

- [ ] **Step 1: 创建 BrowserCacheScanner**

`WinClear/WinClear/Services/ScannerProviders/BrowserCacheScanner.cs`:

```csharp
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
```

- [ ] **Step 2: 创建 AppCacheScanner**

`WinClear/WinClear/Services/ScannerProviders/AppCacheScanner.cs`:

```csharp
using WinClear.Models;
using WinClear.Services;

namespace WinClear.ScannerProviders;

public class AppCacheScanner : IScanner
{
    public string CategoryName => "应用缓存";
    public string SourceApp => "应用";

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
```

- [ ] **Step 3: 验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

---

### Task 6: 大文件和重复文件扫描器 + 用户自定义

**Files:**
- Create: `WinClear/WinClear/Services/ScannerProviders/LargeFileScanner.cs`
- Create: `WinClear/WinClear/Services/ScannerProviders/DuplicateFileScanner.cs`
- Create: `WinClear/WinClear/Services/ScannerProviders/UserDefinedScanner.cs`

- [ ] **Step 1: 创建 LargeFileScanner**

`WinClear/WinClear/Services/ScannerProviders/LargeFileScanner.cs`:

```csharp
using WinClear.Models;
using WinClear.Services;

namespace WinClear.ScannerProviders;

public class LargeFileScanner : IScanner
{
    public string CategoryName => "大文件";
    public string SourceApp => "大文件";

    public long MinSizeBytes { get; set; } = 100L * 1024 * 1024; // 100MB

    public async Task<List<FileItem>> ScanAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var items = new List<FileItem>();
        var drives = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
            .Select(d => d.RootDirectory.FullName);

        foreach (var drive in drives)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var topDirs = Directory.GetDirectories(drive);
                for (int i = 0; i < topDirs.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.Report((double)i / topDirs.Length);

                    try
                    {
                        WalkDirectory(topDirs[i], items, cancellationToken);
                    }
                    catch { }
                }
            }
            catch { }
        }

        return items.OrderByDescending(f => f.SizeBytes).Take(500).ToList();
    }

    private void WalkDirectory(string dirPath, List<FileItem> items, CancellationToken ct)
    {
        try
        {
            foreach (var file in Directory.GetFiles(dirPath))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var info = new FileInfo(file);
                    if (info.Length >= MinSizeBytes)
                    {
                        items.Add(new FileItem
                        {
                            Name = info.Name,
                            FullPath = info.FullName,
                            SizeBytes = info.Length,
                            Category = CategoryName,
                            SourceApp = SourceApp,
                            SafetyTag = SafetyTag.Warning
                        });
                    }
                }
                catch { }
            }

            foreach (var subDir in Directory.GetDirectories(dirPath))
            {
                ct.ThrowIfCancellationRequested();
                WalkDirectory(subDir, items, ct);
            }
        }
        catch { }
    }
}
```

- [ ] **Step 2: 创建 DuplicateFileScanner**

`WinClear/WinClear/Services/ScannerProviders/DuplicateFileScanner.cs`:

```csharp
using System.Security.Cryptography;
using WinClear.Models;
using WinClear.Services;

namespace WinClear.ScannerProviders;

public class DuplicateFileScanner : IScanner
{
    public string CategoryName => "重复文件";
    public string SourceApp => "重复文件";

    public async Task<List<FileItem>> ScanAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var allFiles = new List<FileInfo>();
        var drives = DriveInfo.GetDrives()
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
```

- [ ] **Step 3: 创建 UserDefinedScanner**

`WinClear/WinClear/Services/ScannerProviders/UserDefinedScanner.cs`:

```csharp
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
```

- [ ] **Step 4: 验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

---

### Task 7: 扫描引擎

**Files:**
- Modify: `WinClear/WinClear/Services/ScanEngine.cs`

- [ ] **Step 1: 创建 ScanEngine**

`WinClear/WinClear/Services/ScanEngine.cs`:

```csharp
using WinClear.Models;
using WinClear.ScannerProviders;

namespace WinClear.Services;

public class ScanEngine
{
    private readonly List<IScanner> _scanners;

    public ScanEngine()
    {
        _scanners = new List<IScanner>
        {
            new SystemTempScanner(),
            new WindowsUpdateScanner(),
            new BrowserCacheScanner(),
            new AppCacheScanner(),
            new LargeFileScanner(),
            new DuplicateFileScanner(),
        };
    }

    public UserDefinedScanner UserDefinedScanner { get; } = new();

    public async Task<ScanResult> RunScanAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var result = new ScanResult();
        var allResults = new List<List<FileItem>>();

        for (int i = 0; i < _scanners.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scanner = _scanners[i];
            var items = await scanner.ScanAsync(
                progress == null ? null : new Progress<double>(p =>
                    progress.Report((i + p) / (_scanners.Count + (UserDefinedScanner.CustomPaths.Count > 0 ? 1 : 0)))),
                cancellationToken);

            if (items.Count > 0)
            {
                var categoryNode = new FileItem
                {
                    Name = scanner.CategoryName,
                    FullPath = scanner.CategoryName,
                    IsSelected = true,
                    Category = scanner.CategoryName,
                    SourceApp = scanner.SourceApp,
                    SizeBytes = items.Sum(f => f.SizeBytes),
                    SafetyTag = SafetyTag.Safe,
                };
                categoryNode.Children = new System.Collections.ObjectModel.ObservableCollection<FileItem>(items);
                result.Categories.Add(categoryNode);
                result.TotalSize += categoryNode.SizeBytes;
                result.TotalFiles += items.Count;
            }
        }

        // User-defined scanner
        if (UserDefinedScanner.CustomPaths.Count > 0)
        {
            var udItems = await UserDefinedScanner.ScanAsync(progress, cancellationToken);
            if (udItems.Count > 0)
            {
                var categoryNode = new FileItem
                {
                    Name = UserDefinedScanner.CategoryName,
                    FullPath = UserDefinedScanner.CategoryName,
                    IsSelected = true,
                    Category = UserDefinedScanner.CategoryName,
                    SourceApp = UserDefinedScanner.SourceApp,
                    SizeBytes = udItems.Sum(f => f.SizeBytes),
                    SafetyTag = SafetyTag.Safe,
                };
                categoryNode.Children = new System.Collections.ObjectModel.ObservableCollection<FileItem>(udItems);
                result.Categories.Add(categoryNode);
                result.TotalSize += categoryNode.SizeBytes;
                result.TotalFiles += udItems.Count;
            }
        }

        return result;
    }
}
```

- [ ] **Step 2: 验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

---

### Task 8: WPF 值转换器

**Files:**
- Create: `WinClear/WinClear/Converters/SafetyTagColorConverter.cs`
- Create: `WinClear/WinClear/Converters/BoolToVisibilityConverter.cs`

- [ ] **Step 1: 创建 SafetyTagColorConverter**

`WinClear/WinClear/Converters/SafetyTagColorConverter.cs`:

```csharp
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WinClear.Models;

namespace WinClear.Converters;

[ValueConversion(typeof(SafetyTag), typeof(Brush))]
public class SafetyTagColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SafetyTag tag)
        {
            return tag switch
            {
                SafetyTag.Safe => new SolidColorBrush(Color.FromRgb(76, 175, 80)),    // Green
                SafetyTag.Warning => new SolidColorBrush(Color.FromRgb(255, 193, 7)),  // Amber
                SafetyTag.Danger => new SolidColorBrush(Color.FromRgb(244, 67, 54)),   // Red
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

- [ ] **Step 2: 创建 FileSizeConverter**

`WinClear/WinClear/Converters/FileSizeConverter.cs`:

```csharp
using System.Globalization;
using System.Windows.Data;
using WinClear.Helpers;

namespace WinClear.Converters;

[ValueConversion(typeof(long), typeof(string))]
public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytes)
            return FileSizeFormatter.Format(bytes);
        return "0 B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

- [ ] **Step 3: 创建 BoolToVisibilityConverter**

`WinClear/WinClear/Converters/BoolToVisibilityConverter.cs`:

```csharp
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WinClear.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}
```

- [ ] **Step 4: 验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

---

### Task 9: MainViewModel

**Files:**
- Create: `WinClear/WinClear/ViewModels/MainViewModel.cs`

- [ ] **Step 1: 创建 MainViewModel**

`WinClear/WinClear/ViewModels/MainViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinClear.Models;
using WinClear.Services;

namespace WinClear.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ScanEngine _scanEngine;

    public MainViewModel()
    {
        _scanEngine = new ScanEngine();
    }

    [ObservableProperty]
    private ObservableCollection<FileItem> _categories = new();

    [ObservableProperty]
    private FileItem? _selectedCategory;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private double _scanProgress;

    [ObservableProperty]
    private long _totalSize;

    [ObservableProperty]
    private int _totalFiles;

    [ObservableProperty]
    private long _selectedSize;

    [ObservableProperty]
    private int _selectedCount;

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        IsScanning = true;
        ScanProgress = 0;
        StatusText = "正在扫描...";
        Categories.Clear();

        try
        {
            var progress = new Progress<double>(p =>
            {
                ScanProgress = p;
            });

            var result = await _scanEngine.RunScanAsync(progress, CancellationToken.None);

            Categories = result.Categories;
            TotalSize = result.TotalSize;
            TotalFiles = result.TotalFiles;

            StatusText = $"扫描完成 - 共 {result.TotalFiles} 项，{FileSizeFormatter.Format(result.TotalSize)}";
            UpdateSelectedStats();
        }
        catch (Exception ex)
        {
            StatusText = $"扫描出错: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            ScanCommand.NotifyCanExecuteChanged();
            DeleteSelectedCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanScan() => !IsScanning;

    [RelayCommand]
    private void SelectAll()
    {
        SetAllSelection(Categories, true);
        UpdateSelectedStats();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        SetAllSelection(Categories, false);
        UpdateSelectedStats();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private async Task DeleteSelectedAsync()
    {
        var toDelete = GetSelectedFiles(Categories).ToList();

        if (toDelete.Count == 0)
        {
            StatusText = "没有选中任何文件";
            return;
        }

        var warningCount = toDelete.Count(f => f.SafetyTag == SafetyTag.Danger);
        var totalSize = toDelete.Sum(f => f.SizeBytes);

        var message = $"确定要删除选中的 {toDelete.Count} 个文件 ({FileSizeFormatter.Format(totalSize)})？";
        if (warningCount > 0)
            message += $"\n\n⚠️ 其中有 {warningCount} 个标记为危险的文件！";

        var result = System.Windows.MessageBox.Show(
            message, "确认删除",
            System.Windows.MessageBoxButton.YesNo,
            warningCount > 0
                ? System.Windows.MessageBoxImage.Warning
                : System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        int successCount = 0;
        int failCount = 0;

        foreach (var file in toDelete)
        {
            try
            {
                if (File.Exists(file.FullPath))
                    File.Delete(file.FullPath);
                else if (Directory.Exists(file.FullPath))
                    Directory.Delete(file.FullPath, true);
                successCount++;
            }
            catch
            {
                failCount++;
            }
        }

        System.Windows.MessageBox.Show(
            $"清理完成\n成功: {successCount} 项\n失败: {failCount} 项",
            "清理结果",
            System.Windows.MessageBoxButton.OK,
            failCount > 0 ? System.Windows.MessageBoxImage.Warning : System.Windows.MessageBoxImage.Information);

        StatusText = $"清理完成 - 成功 {successCount} 项，失败 {failCount} 项";
        UpdateSelectedStats();
    }

    private void SetAllSelection(IEnumerable<FileItem> items, bool selected)
    {
        foreach (var item in items)
        {
            item.IsSelected = selected;
            if (item.Children.Count > 0)
                SetAllSelection(item.Children, selected);
        }
    }

    private bool CanDeleteSelected() => SelectedCount > 0;

    private IEnumerable<FileItem> GetSelectedFiles(IEnumerable<FileItem> items)
    {
        foreach (var item in items)
        {
            if (item.Children.Count > 0)
            {
                foreach (var child in GetSelectedFiles(item.Children))
                    yield return child;
            }
            else if (item.IsSelected)
            {
                yield return item;
            }
        }
    }

    private void UpdateSelectedStats()
    {
        var selected = GetSelectedFiles(Categories).ToList();
        SelectedCount = selected.Count;
        SelectedSize = selected.Sum(f => f.SizeBytes);
        DeleteSelectedCommand.NotifyCanExecuteChanged();
    }
}
```

- [ ] **Step 2: 验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

---

### Task 10: MainWindow XAML 界面

**Files:**
- Write: `WinClear/WinClear/MainWindow.xaml`
- Write: `WinClear/WinClear/MainWindow.xaml.cs`

- [ ] **Step 1: 编写 MainWindow.xaml**

`WinClear/WinClear/MainWindow.xaml`:

```xml
<Window x:Class="WinClear.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:WinClear.ViewModels"
        xmlns:conv="clr-namespace:WinClear.Converters"
        Title="WinClear - 系统垃圾清理"
        Width="900" Height="600"
        MinWidth="700" MinHeight="450"
        WindowStartupLocation="CenterScreen">

    <Window.Resources>
        <conv:SafetyTagColorConverter x:Key="SafetyTagColorConverter"/>
        <conv:BoolToVisibilityConverter x:Key="BoolToVisibilityConverter"/>
        <conv:FileSizeConverter x:Key="FileSizeConverter"/>
    </Window.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 工具栏 -->
        <ToolBar Grid.Row="0">
            <Button Command="{Binding ScanCommand}">
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="🔍 " />
                    <TextBlock Text="扫描" />
                </StackPanel>
            </Button>
            <Separator/>
            <Button Command="{Binding SelectAllCommand}">全选</Button>
            <Button Command="{Binding DeselectAllCommand}">反选</Button>
            <Separator/>
            <Button Command="{Binding DeleteSelectedCommand}">
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="🗑️ " />
                    <TextBlock Text="删除选中" />
                </StackPanel>
            </Button>
        </ToolBar>

        <!-- 主区域: 左侧分类树 + 右侧文件列表 -->
        <Grid Grid.Row="1" Margin="5">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="250"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- 左侧: 分类树 -->
            <Border Grid.Column="0" BorderBrush="#E0E0E0" BorderThickness="1" Margin="0,0,5,0">
                <TreeView ItemsSource="{Binding Categories}" SelectedItemChanged="TreeView_OnSelectedItemChanged">
                    <TreeView.ItemTemplate>
                        <HierarchicalDataTemplate ItemsSource="{Binding Children}">
                            <StackPanel Orientation="Horizontal">
                                <TextBlock Text="{Binding Name}" FontWeight="Bold"/>
                                <TextBlock Text=" (" Margin="3,0,0,0"/>
                                <TextBlock Text="{Binding SizeBytes, Converter={StaticResource FileSizeConverter}}"/>
                                <TextBlock Text=")"/>
                            </StackPanel>
                        </HierarchicalDataTemplate>
                    </TreeView.ItemTemplate>
                </TreeView>
            </Border>

            <!-- 右侧: 文件列表 -->
            <Border Grid.Column="1" BorderBrush="#E0E0E0" BorderThickness="1">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>

                    <!-- 文件列表头部 -->
                    <Border Grid.Row="0" Background="#F5F5F5" Padding="5">
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="30"/>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="80"/>
                                <ColumnDefinition Width="60"/>
                            </Grid.ColumnDefinitions>
                            <TextBlock Grid.Column="1" FontWeight="SemiBold">文件名</TextBlock>
                            <TextBlock Grid.Column="2" FontWeight="SemiBold" TextAlignment="Right">大小</TextBlock>
                            <TextBlock Grid.Column="3" FontWeight="SemiBold" TextAlignment="Center">影响</TextBlock>
                        </Grid>
                    </Border>

                    <!-- 文件列表项 -->
                    <ListView Grid.Row="1" ItemsSource="{Binding CurrentFileItems}"
                              ScrollViewer.HorizontalScrollBarVisibility="Disabled">
                        <ListView.ItemTemplate>
                            <DataTemplate>
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="30"/>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="80"/>
                                        <ColumnDefinition Width="60"/>
                                    </Grid.ColumnDefinitions>

                                    <CheckBox Grid.Column="0"
                                              IsChecked="{Binding IsSelected}"
                                              VerticalAlignment="Center"/>

                                    <TextBlock Grid.Column="1" Text="{Binding Name}"
                                               TextTrimming="CharacterEllipsis"
                                               VerticalAlignment="Center"/>

                                    <TextBlock Grid.Column="2"
                                               Text="{Binding SizeBytes, Converter={StaticResource FileSizeConverter}}"
                                               TextAlignment="Right"
                                               VerticalAlignment="Center"/>

                                    <Ellipse Grid.Column="3" Width="12" Height="12"
                                             Fill="{Binding SafetyTag, Converter={StaticResource SafetyTagColorConverter}}"
                                             Margin="0,0,0,0"
                                             HorizontalAlignment="Center"
                                             ToolTip="{Binding SafetyTag}"/>
                                </Grid>
                            </DataTemplate>
                        </ListView.ItemTemplate>
                    </ListView>
                </Grid>
            </Border>
        </Grid>

        <!-- 进度条 -->
        <ProgressBar Grid.Row="2" Height="4"
                     Value="{Binding ScanProgress}"
                     Visibility="{Binding IsScanning, Converter={StaticResource BoolToVisibilityConverter}}"
                     Margin="5,0"/>

        <!-- 状态栏 -->
        <StatusBar Grid.Row="3">
            <StatusBar.ItemsPanel>
                <ItemsPanelTemplate>
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                    </Grid>
                </ItemsPanelTemplate>
            </StatusBar.ItemsPanel>
            <StatusBarItem>
                <TextBlock Text="{Binding StatusText}"/>
            </StatusBarItem>
            <StatusBarItem Grid.Column="1">
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="🟢 " ToolTip="可安全删除"/>
                    <TextBlock Text="  🟡 " ToolTip="删除可能影响"/>
                    <TextBlock Text="  🔴 " ToolTip="谨慎操作"/>
                </StackPanel>
            </StatusBarItem>
        </StatusBar>
    </Grid>
</Window>
```

- [ ] **Step 2: 编写 MainWindow.xaml.cs**

`WinClear/WinClear/MainWindow.xaml.cs`:

```csharp
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using WinClear.Models;
using WinClear.ViewModels;

namespace WinClear;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is FileItem selectedItem)
        {
            _viewModel.CurrentFileItems = selectedItem.Children;
        }
    }
}
```

- [ ] **Step 3: 在 MainViewModel 中补加 CurrentFileItems**

在 `MainViewModel.cs` 的属性区域添加：

```csharp
[ObservableProperty]
private ObservableCollection<FileItem>? _currentFileItems;
```

- [ ] **Step 4: 验证构建**

```bash
dotnet build
```
Expected: Build succeeded.

---

### Task 11: App 启动引导

**Files:**
- Modify: `WinClear/WinClear/App.xaml`
- Modify: `WinClear/WinClear/App.xaml.cs`

- [ ] **Step 1: 修改 App.xaml**

将 StartupUri 移除，因为我们会在代码中手动创建窗口。

`WinClear/WinClear/App.xaml`:

```xml
<Application x:Class="WinClear.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
</Application>
```

- [ ] **Step 2: 修改 App.xaml.cs**

`WinClear/WinClear/App.xaml.cs`:

```csharp
using Serilog;
using System.Windows;

namespace WinClear;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug()
            .WriteTo.File(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WinClear", "logs", "log-.txt"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("WinClear 启动");

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
```

- [ ] **Step 3: 验证完整构建**

```bash
dotnet build
```
Expected: Build succeeded.

- [ ] **Step 4: 运行测试**

```bash
dotnet run --project WinClear/WinClear
```
Expected: 应用窗口正常打开，点击"扫描"可开始扫描。

---

### Task 12: 创建 AGENTS.md（项目约定的编码习惯）

**Files:**
- Create: `WinClear/AGENTS.md`

- [ ] **Step 1: 写入 AGENTS.md**

`WinClear/AGENTS.md`:

```markdown
# WinClear 项目约定

## 技术栈
- C# .NET 8 + WPF + CommunityToolkit.Mvvm

## 代码风格
- 使用 CommunityToolkit.Mvvm 的 source generator（[ObservableProperty]、[RelayCommand]）
- namespace 使用文件范围（`namespace WinClear.xxx;`）
- Services 下接口以 I 开头
- Scanner 实现 IScanner 接口，放在 ScannerProviders 目录
- XAML 中的 Converter 放在 Converters 目录

## 构建命令
- dotnet build
- dotnet run --project WinClear/WinClear
```

---

## 自我审查清单

- [x] **设计覆盖度**: 所有 spec 中的 Scanner + UI + ViewModel + 删除流程都已覆盖
- [x] **占位符检查**: 无 TBD/TODO，代码完整可构建
- [x] **类型一致性**: IScanner 接口在所有实现中保持一致，Model 字段在 ViewModel 和 XAML 中一致引用
- [x] **Scope 检查**: 单个 WPF 项目，范围合理
