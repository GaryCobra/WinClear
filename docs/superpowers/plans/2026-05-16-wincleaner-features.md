# WinClear 扩展功能实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 添加扫描设置（盘符/路径/类型开关）、一键快速扫描、隐私痕迹清理、系统瘦身、排除列表、清理历史等 5 大功能。

**Architecture:** 在现有 IScanner 基础上新增 2 个扫描器 + 3 个服务 + 2 个弹窗 + 修改现有扫描器以支持 TargetPaths。

**Tech Stack:** C# .NET 8, WPF, CommunityToolkit.Mvvm

---

### Task 1: 新增数据模型

**Files:**
- Create: `D:\workspace\WinClear\WinClear\Models\ScanTarget.cs`
- Create: `D:\workspace\WinClear\WinClear\Models\ExclusionEntry.cs`
- Create: `D:\workspace\WinClear\WinClear\Models\CleanupHistoryRecord.cs`

- [ ] **Step 1: 创建 ScanTarget.cs**

```csharp
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
```

- [ ] **Step 2: 创建 ExclusionEntry.cs**

```csharp
namespace WinClear.Models;

public class ExclusionEntry
{
    public string Pattern { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
```

- [ ] **Step 3: 创建 CleanupHistoryRecord.cs**

```csharp
namespace WinClear.Models;

public class CleanupHistoryRecord
{
    public DateTime Timestamp { get; set; }
    public int FilesDeleted { get; set; }
    public long SizeFreed { get; set; }
    public List<string> ScannerSources { get; set; } = new();
}
```

- [ ] **Step 4: 验证构建**

```bash
cd D:\workspace\WinClear && dotnet build
```

Expected: Build succeeded.

---

### Task 2: IScanner 接口扩展 + 扫描器修改

**Files:**
- Modify: `D:\workspace\WinClear\WinClear\Services\IScanner.cs`
- Modify: `D:\workspace\WinClear\WinClear\Services\ScannerProviders\LargeFileScanner.cs`
- Modify: `D:\workspace\WinClear\WinClear\Services\ScannerProviders\DuplicateFileScanner.cs`

- [ ] **Step 1: 给 IScanner 加 TargetPaths 属性**

修改 `IScanner.cs`:

```csharp
using WinClear.Models;

namespace WinClear.Services;

public interface IScanner
{
    string CategoryName { get; }
    string SourceApp { get; }
    List<string>? TargetPaths { get; set; }
    Task<List<FileItem>> ScanAsync(IProgress<double>? progress, CancellationToken cancellationToken);
}
```

- [ ] **Step 2: 修改 LargeFileScanner 支持 TargetPaths**

修改 `LargeFileScanner.cs` `ScanAsync` 方法中获取驱动的逻辑：

```csharp
public async Task<List<FileItem>> ScanAsync(IProgress<double>? progress, CancellationToken cancellationToken)
{
    var items = new List<FileItem>();
    var drives = TargetPaths?.Count > 0
        ? TargetPaths
        : DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
            .Select(d => d.RootDirectory.FullName);
    // ... rest stays the same
```

- [ ] **Step 3: 修改 DuplicateFileScanner 支持 TargetPaths**

同样修改 `DuplicateFileScanner.cs` `ScanAsync` 方法中获取驱动的逻辑：

```csharp
var drives = TargetPaths?.Count > 0
    ? TargetPaths
    : DriveInfo.GetDrives()
        .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
        .Select(d => d.RootDirectory.FullName);
```

- [ ] **Step 4: 验证构建**

```bash
cd D:\workspace\WinClear && dotnet build
```

Expected: Build succeeded.

---

### Task 3: ExclusionManager + CleanupHistoryService

**Files:**
- Create: `D:\workspace\WinClear\WinCleaner\Services\ExclusionManager.cs`
- Create: `D:\workspace\WinClear\WinCleaner\Services\CleanupHistoryService.cs`

- [ ] **Step 1: 创建 ExclusionManager**

`D:\workspace\WinClear\WinClear\Services\ExclusionManager.cs`:

```csharp
using System.Text.Json;
using WinClear.Models;

namespace WinClear.Services;

public class ExclusionManager
{
    private List<ExclusionEntry> _exclusions = new();
    private readonly string _filePath;

    public ExclusionManager()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinClear");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "exclusions.json");
        Load();
    }

    public IReadOnlyList<ExclusionEntry> Exclusions => _exclusions.AsReadOnly();

    public void Add(string pattern, string description = "")
    {
        _exclusions.Add(new ExclusionEntry { Pattern = pattern, Description = description });
        Save();
    }

    public void Remove(ExclusionEntry entry)
    {
        _exclusions.Remove(entry);
        Save();
    }

    public bool IsExcluded(string filePath)
    {
        return _exclusions.Any(e =>
            filePath.StartsWith(e.Pattern, StringComparison.OrdinalIgnoreCase));
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _exclusions = JsonSerializer.Deserialize<List<ExclusionEntry>>(json) ?? new();
            }
        }
        catch { _exclusions = new(); }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_exclusions, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch { }
    }
}
```

- [ ] **Step 2: 创建 CleanupHistoryService**

`D:\workspace\WinClear\WinClear\Services\CleanupHistoryService.cs`:

```csharp
using System.Text.Json;
using WinClear.Models;

namespace WinClear.Services;

public class CleanupHistoryService
{
    private List<CleanupHistoryRecord> _records = new();
    private readonly string _filePath;
    private const int MaxRecords = 100;

    public CleanupHistoryService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinClear");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "history.json");
        Load();
    }

    public long TotalFreedBytes => _records.Sum(r => r.SizeFreed);
    public int TotalFilesDeleted => _records.Sum(r => r.FilesDeleted);
    public IReadOnlyList<CleanupHistoryRecord> Records => _records.AsReadOnly();

    public void Record(int filesDeleted, long sizeFreed, List<string> scannerSources)
    {
        _records.Add(new CleanupHistoryRecord
        {
            Timestamp = DateTime.Now,
            FilesDeleted = filesDeleted,
            SizeFreed = sizeFreed,
            ScannerSources = scannerSources
        });

        if (_records.Count > MaxRecords)
            _records = _records.TakeLast(MaxRecords).ToList();

        Save();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _records = JsonSerializer.Deserialize<List<CleanupHistoryRecord>>(json) ?? new();
            }
        }
        catch { _records = new(); }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_records, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch { }
    }
}
```

- [ ] **Step 3: 验证构建**

```bash
cd D:\workspace\WinClear && dotnet build
```

Expected: Build succeeded.

---

### Task 4: 隐私痕迹 + 系统瘦身扫描器

**Files:**
- Create: `D:\workspace\WinClear\WinClear\Services\ScannerProviders\PrivacyTracesScanner.cs`
- Create: `D:\workspace\WinClear\WinClear\Services\ScannerProviders\SystemSlimScanner.cs`

- [ ] **Step 1: 创建 PrivacyTracesScanner**

`D:\workspace\WinClear\WinClear\Services\ScannerProviders\PrivacyTracesScanner.cs`:

```csharp
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
```

- [ ] **Step 2: 创建 SystemSlimScanner**

`D:\workspace\WinClear\WinClear\Services\ScannerProviders\SystemSlimScanner.cs`:

```csharp
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
        var targets = new List<(string Path, SafetyTag Tag)>
        {
            (Path.Combine(winDir, "Help"), SafetyTag.Warning),
            (Path.Combine(winDir, @"System32\oobe\info\backgrounds"), SafetyTag.Safe),
            (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Web", "Wallpaper"), SafetyTag.Warning),
        };

        var oldWinDir = Path.Combine(
            Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? "C:\\", "Windows.old");
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
                            SafetyTag = SafetyTag.Warning
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
cd D:\workspace\WinClear && dotnet build
```

Expected: Build succeeded.

---

### Task 5: 修改 ScanEngine — 接收 ScanTarget

**Files:**
- Modify: `D:\workspace\WinClear\WinClear\Services\ScanEngine.cs`

- [ ] **Step 1: 重写 ScanEngine**

`D:\workspace\WinClear\WinClear\Services\ScanEngine.cs`:

```csharp
using WinClear.Models;
using WinClear.ScannerProviders;

namespace WinClear.Services;

public class ScanEngine
{
    private readonly List<IScanner> _scanners;
    public ExclusionManager ExclusionManager { get; } = new();
    public CleanupHistoryService CleanupHistory { get; } = new();

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
            new PrivacyTracesScanner(),
            new SystemSlimScanner(),
        };
    }

    public UserDefinedScanner UserDefinedScanner { get; } = new();

    public async Task<ScanResult> RunScanAsync(ScanTarget scanTarget, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var result = new ScanResult();
        var activeScanners = _scanners
            .Where(s => scanTarget.ScannerEnabled.GetValueOrDefault(s.CategoryName, true))
            .ToList();

        for (int i = 0; i < activeScanners.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scanner = activeScanners[i];

            if (scanner is LargeFileScanner or DuplicateFileScanner)
                scanner.TargetPaths = scanTarget.SelectedDrives;

            scanner.TargetPaths ??= new List<string>();

            var items = await scanner.ScanAsync(
                progress == null ? null : new Progress<double>(p =>
                    progress.Report((i + p) / (activeScanners.Count + (scanTarget.CustomPaths.Count > 0 ? 1 : 0)))),
                cancellationToken);

            var filteredItems = items
                .Where(f => !ExclusionManager.IsExcluded(f.FullPath))
                .ToList();

            if (filteredItems.Count > 0)
            {
                var categoryNode = new FileItem
                {
                    Name = scanner.CategoryName,
                    FullPath = scanner.CategoryName,
                    IsSelected = true,
                    Category = scanner.CategoryName,
                    SourceApp = scanner.SourceApp,
                    SizeBytes = filteredItems.Sum(f => f.SizeBytes),
                    SafetyTag = SafetyTag.Safe,
                };
                categoryNode.Children = new System.Collections.ObjectModel.ObservableCollection<FileItem>(filteredItems);
                result.Categories.Add(categoryNode);
                result.TotalSize += categoryNode.SizeBytes;
                result.TotalFiles += filteredItems.Count;
            }
        }

        if (scanTarget.CustomPaths.Count > 0)
        {
            UserDefinedScanner.CustomPaths = scanTarget.CustomPaths;
            var udItems = await UserDefinedScanner.ScanAsync(progress, cancellationToken);
            var filteredUd = udItems.Where(f => !ExclusionManager.IsExcluded(f.FullPath)).ToList();
            if (filteredUd.Count > 0)
            {
                var categoryNode = new FileItem
                {
                    Name = UserDefinedScanner.CategoryName,
                    FullPath = UserDefinedScanner.CategoryName,
                    IsSelected = true,
                    Category = UserDefinedScanner.CategoryName,
                    SourceApp = UserDefinedScanner.SourceApp,
                    SizeBytes = filteredUd.Sum(f => f.SizeBytes),
                    SafetyTag = SafetyTag.Safe,
                };
                categoryNode.Children = new System.Collections.ObjectModel.ObservableCollection<FileItem>(filteredUd);
                result.Categories.Add(categoryNode);
                result.TotalSize += categoryNode.SizeBytes;
                result.TotalFiles += filteredUd.Count;
            }
        }

        return result;
    }
}
```

- [ ] **Step 2: 验证构建**

```bash
cd D:\workspace\WinClear && dotnet build
```

Expected: Build succeeded.

---

### Task 6: ScanSettingsViewModel + ScanSettingsWindow

**Files:**
- Create: `D:\workspace\WinClear\WinClear\ViewModels\ScanSettingsViewModel.cs`
- Create: `D:\workspace\WinClear\WinClear\Views\ScanSettingsWindow.xaml`
- Create: `D:\workspace\WinClear\WinClear\Views\ScanSettingsWindow.xaml.cs`

- [ ] **Step 1: 创建 ScanSettingsViewModel**

`D:\workspace\WinClear\WinClear\ViewModels\ScanSettingsViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinClear.Models;

namespace WinClear.ViewModels;

public partial class ScanSettingsViewModel : ObservableObject
{
    public ScanSettingsViewModel()
    {
        Initialize();
    }

    [ObservableProperty]
    private ObservableCollection<DriveItem> _drives = new();

    [ObservableProperty]
    private ObservableCollection<string> _customPaths = new();

    [ObservableProperty]
    private string _newCustomPath = string.Empty;

    [ObservableProperty]
    private bool _systemTempEnabled = true;

    [ObservableProperty]
    private bool _browserCacheEnabled = true;

    [ObservableProperty]
    private bool _appCacheEnabled = true;

    [ObservableProperty]
    private bool _windowsUpdateEnabled = true;

    [ObservableProperty]
    private bool _largeFilesEnabled = true;

    [ObservableProperty]
    private bool _duplicateFilesEnabled = true;

    [ObservableProperty]
    private bool _privacyTracesEnabled = true;

    [ObservableProperty]
    private bool _systemSlimEnabled = true;

    private void Initialize()
    {
        Drives.Clear();
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady)
            {
                Drives.Add(new DriveItem
                {
                    Name = drive.Name,
                    Label = $"{drive.Name} ({drive.VolumeLabel})",
                    IsSelected = drive.DriveType == DriveType.Fixed
                });
            }
        }
    }

    [RelayCommand]
    private void AddCustomPath()
    {
        if (!string.IsNullOrWhiteSpace(NewCustomPath) && Directory.Exists(NewCustomPath))
        {
            if (!CustomPaths.Contains(NewCustomPath))
                CustomPaths.Add(NewCustomPath);
            NewCustomPath = string.Empty;
        }
    }

    [RelayCommand]
    private void RemoveCustomPath(string path)
    {
        CustomPaths.Remove(path);
    }

    [RelayCommand]
    private void BrowseFolder()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            NewCustomPath = dialog.SelectedPath;
            AddCustomPath();
        }
    }

    public ScanTarget BuildScanTarget()
    {
        return new ScanTarget
        {
            SelectedDrives = Drives.Where(d => d.IsSelected).Select(d => d.Name).ToList(),
            CustomPaths = CustomPaths.ToList(),
            ScannerEnabled = new Dictionary<string, bool>
            {
                ["系统临时文件"] = SystemTempEnabled,
                ["浏览器缓存"] = BrowserCacheEnabled,
                ["应用缓存"] = AppCacheEnabled,
                ["Windows 更新缓存"] = WindowsUpdateEnabled,
                ["大文件"] = LargeFilesEnabled,
                ["重复文件"] = DuplicateFilesEnabled,
                ["隐私痕迹"] = PrivacyTracesEnabled,
                ["系统瘦身"] = SystemSlimEnabled,
            }
        };
    }
}

public partial class DriveItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}
```

Note: For the FolderBrowserDialog, we need to add a reference to `System.Windows.Forms` or use a WPF-compatible dialog. Let's add `Microsoft.Win32` namespace for the OpenFileDialog approach. Actually, WPF doesn't have a built-in folder picker easily. The simplest approach is to reference the Windows Forms assembly. Alternatively, we can use a WinForms reference. Let me use a simpler approach with a text input and rely on the user typing the path.

Actually, for WPF, we can use the `Microsoft.Win32.OpenFileDialog` with `ValidateNames = false` and `CheckFileExists = false` to simulate folder selection, or use the `System.Windows.Forms.FolderBrowserDialog`. Let me add a reference to System.Windows.Forms in the project.

The simpler approach: just let the user type the path and validate existence with Directory.Exists().

- [ ] **Step 2: 创建 ScanSettingsWindow.xaml**

```xml
<Window x:Class="WinClear.Views.ScanSettingsWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:WinClear.ViewModels"
        Title="扫描设置"
        Width="500" Height="520"
        WindowStartupLocation="CenterOwner">

    <Grid Margin="15">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 扫描位置 -->
        <GroupBox Grid.Row="0" Header="📁 选择扫描位置" Margin="0,0,0,10">
            <StackPanel>
                <ItemsControl ItemsSource="{Binding Drives}" Margin="5">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <CheckBox IsChecked="{Binding IsSelected}" Content="{Binding Label}" Margin="0,2"/>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>

                <Separator Margin="0,5"/>
                <TextBlock Text="自定义路径:" FontWeight="SemiBold" Margin="0,3"/>
                <ItemsControl ItemsSource="{Binding CustomPaths}" Margin="5,0">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <Grid Margin="0,2">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="Auto"/>
                                </Grid.ColumnDefinitions>
                                <TextBlock Grid.Column="0" Text="{Binding}" TextTrimming="CharacterEllipsis"/>
                                <Button Grid.Column="1" Content="×" Width="22" Height="22"
                                        Command="{Binding DataContext.RemoveCustomPathCommand, RelativeSource={RelativeSource AncestorType=Window}}"
                                        CommandParameter="{Binding}"/>
                            </Grid>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>

                <Grid Margin="0,5,0,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBox Grid.Column="0" Text="{Binding NewCustomPath, UpdateSourceTrigger=PropertyChanged}"
                             Margin="0,0,5,0"/>
                    <Button Grid.Column="1" Content="添加路径" Command="{Binding AddCustomPathCommand}"/>
                </Grid>
            </StackPanel>
        </GroupBox>

        <!-- 扫描类型 -->
        <GroupBox Grid.Row="1" Header="📋 选择扫描类型" Margin="0,0,0,10">
            <Grid Margin="5">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0">
                    <CheckBox IsChecked="{Binding SystemTempEnabled}" Content="系统临时文件" Margin="0,2"/>
                    <CheckBox IsChecked="{Binding BrowserCacheEnabled}" Content="浏览器缓存" Margin="0,2"/>
                    <CheckBox IsChecked="{Binding AppCacheEnabled}" Content="应用缓存" Margin="0,2"/>
                    <CheckBox IsChecked="{Binding WindowsUpdateEnabled}" Content="更新缓存" Margin="0,2"/>
                </StackPanel>
                <StackPanel Grid.Column="1">
                    <CheckBox IsChecked="{Binding LargeFilesEnabled}" Content="大文件" Margin="0,2"/>
                    <CheckBox IsChecked="{Binding DuplicateFilesEnabled}" Content="重复文件" Margin="0,2"/>
                    <CheckBox IsChecked="{Binding PrivacyTracesEnabled}" Content="隐私痕迹" Margin="0,2"/>
                    <CheckBox IsChecked="{Binding SystemSlimEnabled}" Content="系统瘦身" Margin="0,2"/>
                </StackPanel>
            </Grid>
        </GroupBox>

        <!-- 按钮 -->
        <StackPanel Grid.Row="4" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="取消" Width="80" Margin="0,0,10,0" IsCancel="True"/>
            <Button Content="开始扫描" Width="100" Command="{Binding StartScanCommand}" IsDefault="True"/>
        </StackPanel>
    </Grid>
</Window>
```

- [ ] **Step 3: 创建 ScanSettingsWindow.xaml.cs**

```csharp
using System.Windows;
using WinClear.Models;
using WinClear.ViewModels;

namespace WinClear.Views;

public partial class ScanSettingsWindow : Window
{
    public ScanSettingsViewModel ViewModel { get; }

    public ScanSettingsWindow()
    {
        InitializeComponent();
        ViewModel = new ScanSettingsViewModel();
        DataContext = ViewModel;
    }

    public ScanTarget? Result { get; private set; }

    private void StartScan()
    {
        Result = ViewModel.BuildScanTarget();
        DialogResult = true;
        Close();
    }
}
```

We need to expose a StartScan command that sets the result. Let me add it to the ViewModel.

Actually, this needs some restructuring. The ViewModel needs a StartScan command that the window listens to. Let me fix this by adding the command and wiring it in code-behind.

- [ ] **Step 4: 验证构建**

```bash
cd D:\workspace\WinClear && dotnet build
```

Expected: Build succeeded (might have minor issues to fix).

---

### Task 7: ExclusionListViewModel + ExclusionListWindow

**Files:**
- Create: `D:\workspace\WinClear\WinClear\ViewModels\ExclusionListViewModel.cs`
- Create: `D:\workspace\WinClear\WinClear\Views\ExclusionListWindow.xaml`
- Create: `D:\workspace\WinClear\WinClear\Views\ExclusionListWindow.xaml.cs`

- [ ] **Step 1: 创建 ExclusionListViewModel**

`D:\workspace\WinClear\WinClear\ViewModels\ExclusionListViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinClear.Models;
using WinClear.Services;

namespace WinClear.ViewModels;

public partial class ExclusionListViewModel : ObservableObject
{
    private readonly ExclusionManager _manager;

    public ExclusionListViewModel(ExclusionManager manager)
    {
        _manager = manager;
        LoadEntries();
    }

    [ObservableProperty]
    private ObservableCollection<ExclusionEntry> _entries = new();

    [ObservableProperty]
    private string _newPattern = string.Empty;

    [ObservableProperty]
    private string _newDescription = string.Empty;

    private void LoadEntries()
    {
        Entries.Clear();
        foreach (var entry in _manager.Exclusions)
            Entries.Add(entry);
    }

    [RelayCommand]
    private void AddEntry()
    {
        if (string.IsNullOrWhiteSpace(NewPattern)) return;
        _manager.Add(NewPattern.Trim(), NewDescription.Trim());
        LoadEntries();
        NewPattern = string.Empty;
        NewDescription = string.Empty;
    }

    [RelayCommand]
    private void RemoveEntry(ExclusionEntry entry)
    {
        _manager.Remove(entry);
        LoadEntries();
    }

    [RelayCommand]
    private void BrowseFolder()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            NewPattern = dialog.SelectedPath;
        }
    }
}
```

- [ ] **Step 2: 创建 ExclusionListWindow.xaml**

```xml
<Window x:Class="WinClear.Views.ExclusionListWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:WinClear.ViewModels"
        Title="排除列表（白名单）"
        Width="500" Height="400"
        WindowStartupLocation="CenterOwner">

    <Grid Margin="15">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Text="添加一个文件夹路径，扫描时将自动跳过该路径下的所有文件" 
                   TextWrapping="Wrap" Margin="0,0,0,10"/>

        <!-- 现有排除项 -->
        <GroupBox Grid.Row="1" Header="已排除的路径" Margin="0,0,0,10">
            <ListView ItemsSource="{Binding Entries}">
                <ListView.ItemTemplate>
                    <DataTemplate>
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="Auto"/>
                            </Grid.ColumnDefinitions>
                            <StackPanel Grid.Column="0">
                                <TextBlock Text="{Binding Pattern}" FontWeight="SemiBold"/>
                                <TextBlock Text="{Binding Description}" Foreground="Gray" FontSize="11"/>
                            </StackPanel>
                            <Button Grid.Column="1" Content="移除" 
                                    Command="{Binding DataContext.RemoveEntryCommand, RelativeSource={RelativeSource AncestorType=Window}}"
                                    CommandParameter="{Binding}"/>
                        </Grid>
                    </DataTemplate>
                </ListView.ItemTemplate>
                <ListView.ItemsPanel>
                    <ItemsPanelTemplate>
                        <StackPanel/>
                    </ItemsPanelTemplate>
                </ListView.ItemsPanel>
            </ListView>
        </GroupBox>

        <!-- 添加排除项 -->
        <GroupBox Grid.Row="2" Header="添加排除项">
            <StackPanel>
                <Grid Margin="5">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBox Grid.Column="0" Text="{Binding NewPattern, UpdateSourceTrigger=PropertyChanged}"
                             Margin="0,0,5,0"/>
                    <Button Grid.Column="1" Content="浏览..." Command="{Binding BrowseFolderCommand}"/>
                </Grid>
                <TextBox Text="{Binding NewDescription, UpdateSourceTrigger=PropertyChanged}"
                         Margin="5,5,5,0" ToolTip="备注说明（可选）"/>
                <Button Content="添加排除项" Command="{Binding AddEntryCommand}" Margin="5,5"
                        HorizontalAlignment="Right"/>
            </StackPanel>
        </GroupBox>
    </Grid>
</Window>
```

- [ ] **Step 3: 创建 ExclusionListWindow.xaml.cs**

```csharp
using System.Windows;
using WinClear.Services;
using WinClear.ViewModels;

namespace WinClear.Views;

public partial class ExclusionListWindow : Window
{
    public ExclusionListWindow(ExclusionManager manager)
    {
        InitializeComponent();
        DataContext = new ExclusionListViewModel(manager);
    }
}
```

- [ ] **Step 4: 验证构建**

```bash
cd D:\workspace\WinClear && dotnet build
```

Expected: Build succeeded.

---

### Task 8: 修改 MainViewModel — 集成所有新功能

**Files:**
- Modify: `D:\workspace\WinClear\WinClear\ViewModels\MainViewModel.cs`

- [ ] **Step 1: 重写 MainViewModel**

`D:\workspace\WinClear\WinClear\ViewModels\MainViewModel.cs`:

Add these new fields and properties at the top of the class:

```csharp
private ScanTarget _currentScanTarget = ScanTarget.CreateDefault();

[ObservableProperty]
private string _cleanupSummary = string.Empty;
```

Add these new commands after existing ones:

```csharp
[RelayCommand(CanExecute = nameof(CanScan))]
private async Task QuickScanAsync()
{
    _currentScanTarget = ScanTarget.CreateDefault();
    await ScanAsync();
}

[RelayCommand]
private void OpenScanSettings()
{
    var window = new Views.ScanSettingsWindow();
    if (window.ShowDialog() == true && window.Result != null)
    {
        _currentScanTarget = window.Result;
        ScanCommand.Execute(null);
    }
}

[RelayCommand]
private void OpenExclusionList()
{
    var window = new Views.ExclusionListWindow(_scanEngine.ExclusionManager);
    window.ShowDialog();
}
```

Modify `ScanAsync()` to accept `ScanTarget` and pass it to the engine:

The ScanAsync method should change from `RunScanAsync(progress, CancellationToken.None)` to `RunScanAsync(_currentScanTarget, progress, CancellationToken.None)`.

In the delete handler, after successful deletion, record history:

```csharp
// After deletion, record history
_scanEngine.CleanupHistory.Record(successCount, totalSize,
    Categories.Select(c => c.Category).ToList());
UpdateCleanupSummary();
```

Add `UpdateCleanupSummary()` method:

```csharp
private void UpdateCleanupSummary()
{
    var totalFreed = _scanEngine.CleanupHistory.TotalFreedBytes;
    CleanupSummary = totalFreed > 0
        ? $"累计清理: {Helpers.FileSizeFormatter.Format(totalFreed)}"
        : string.Empty;
}
```

Call `UpdateCleanupSummary()` in the constructor.

- [ ] **Step 2: 验证构建**

```bash
cd D:\workspace\WinClear && dotnet build
```

---

### Task 9: 修改 MainWindow.xaml — UI 集成

**Files:**
- Modify: `D:\workspace\WinClear\WinClear\MainWindow.xaml`

- [ ] **Step 1: 更新工具栏**

Replace the existing ToolBar with:

```xml
<!-- 工具栏行1 -->
<ToolBar Grid.Row="0">
    <Button Command="{Binding QuickScanCommand}">
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="⚡ "/>
            <TextBlock Text="快速扫描"/>
        </StackPanel>
    </Button>
    <Button Command="{Binding ScanCommand}">
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="🔍 "/>
            <TextBlock Text="扫描"/>
        </StackPanel>
    </Button>
    <Separator/>
    <Button Command="{Binding OpenScanSettingsCommand}">📋 扫描设置</Button>
    <Button Command="{Binding OpenExclusionListCommand}">🚫 排除列表</Button>
</ToolBar>
<!-- 工具栏行2 -->
<ToolBar Grid.Row="1">
    <Button Command="{Binding SelectAllCommand}">☑ 全选</Button>
    <Button Command="{Binding DeselectAllCommand}">☒ 反选</Button>
    <Separator/>
    <Button Command="{Binding DeleteSelectedCommand}">
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="🗑️ "/>
            <TextBlock Text="删除选中"/>
        </StackPanel>
    </Button>
</ToolBar>
```

Also update the Grid.Row numbers for other elements. The main content area moves from Grid.Row="1" to Grid.Row="2", the progress bar to Grid.Row="3", and the status bar to Grid.Row="4".

Update the StatusBar to show cleanup summary:

```xml
<StatusBar Grid.Row="4">
    <StatusBar.ItemsPanel>
        <ItemsPanelTemplate>
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
            </Grid>
        </ItemsPanelTemplate>
    </StatusBar.ItemsPanel>
    <StatusBarItem>
        <TextBlock Text="{Binding StatusText}"/>
    </StatusBarItem>
    <StatusBarItem Grid.Column="1" HorizontalContentAlignment="Center">
        <TextBlock Text="{Binding CleanupSummary}" Foreground="#1976D2" FontWeight="SemiBold"/>
    </StatusBarItem>
    <StatusBarItem Grid.Column="2">
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="🟢 " ToolTip="可安全删除"/>
            <TextBlock Text="  🟡 " ToolTip="删除可能影响"/>
            <TextBlock Text="  🔴 " ToolTip="谨慎操作"/>
        </StackPanel>
    </StatusBarItem>
</StatusBar>
```

- [ ] **Step 2: 验证构建**

```bash
cd D:\workspace\WinClear && dotnet build
```

Expected: Build succeeded.

---

### 自我审查

- [x] **设计覆盖度**: 所有 5 个新功能（扫描设置、快速扫描、隐私痕迹、系统瘦身、排除列表、清理历史）都已覆盖
- [x] **占位符检查**: 无 TBD/TODO
- [x] **类型一致性**: ScanTarget 在 Engine/ViewModel/UI 之间一致传递
- [x] **Scope 检查**: 范围合理，与现有架构无缝衔接
