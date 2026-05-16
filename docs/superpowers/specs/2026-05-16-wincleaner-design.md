# WinClear - Windows 垃圾清理桌面应用设计文档

## 概述

WinClear 是一款面向 Windows 10/11 的精简系统垃圾清理桌面应用，帮助用户识别并清理日常软件使用过程中产生的无用临时文件、缓存等占硬盘空间但无实质用途的数据，释放 C 盘存储空间。

## 技术栈

- **框架**: C# WPF (.NET 8)
- **架构模式**: MVVM（Model-View-ViewModel）
- **MVVM 库**: CommunityToolkit.Mvvm
- **日志**: Serilog

## 项目结构

```
WinClear.sln
└─ WinClear/
   ├─ App.xaml / App.xaml.cs              # WPF 入口
   ├─ MainWindow.xaml / MainWindow.xaml.cs
   ├─ Models/
   │   ├─ ScanResult.cs                   # 扫描结果聚合
   │   ├─ FileItem.cs                     # 文件/文件夹数据模型
   │   ├─ SafetyTag.cs                    # 安全等级枚举
   │   ├─ ScanTarget.cs                   # 扫描配置（盘符/路径/类型开关）
   │   ├─ ExclusionEntry.cs              # 排除项模型
   │   └─ CleanupHistoryRecord.cs         # 清理历史记录模型
   ├─ ViewModels/
   │   ├─ MainViewModel.cs                # 主窗口 ViewModel
   │   ├─ ScanSettingsViewModel.cs        # 扫描设置弹窗 ViewModel
   │   └─ ExclusionListViewModel.cs       # 排除列表弹窗 ViewModel
   ├─ Views/
   │   ├─ ScanSettingsWindow.xaml / .cs   # 扫描设置弹窗
   │   └─ ExclusionListWindow.xaml / .cs  # 排除列表弹窗
   ├─ Services/
   │   ├── IScanner.cs                    # 扫描器接口
   │   ├── ScanEngine.cs                  # 扫描引擎
   │   ├── SafetyClassifier.cs            # 安全等级分类器
   │   ├── ExclusionManager.cs            # 排除列表管理
   │   └── CleanupHistoryService.cs       # 清理历史管理
   ├─ Services/ScannerProviders/
   │   ├── SystemTempScanner.cs           # 系统临时文件扫描
   │   ├── BrowserCacheScanner.cs         # 浏览器缓存扫描
   │   ├── AppCacheScanner.cs             # 应用缓存扫描
   │   ├── WindowsUpdateScanner.cs        # Windows 更新缓存扫描
   │   ├── LargeFileScanner.cs            # 大文件扫描
   │   ├── DuplicateFileScanner.cs        # 重复文件扫描
   │   ├── UserDefinedScanner.cs          # 用户自定义路径扫描
   │   ├── PrivacyTracesScanner.cs        # 隐私痕迹扫描
   │   └── SystemSlimScanner.cs           # 系统瘦身扫描
   ├─ Helpers/
   │   └── FileSizeFormatter.cs           # 文件大小格式化
   └─ Converters/
       ├── SafetyTagColorConverter.cs     # 安全等级颜色转换器
       ├── FileSizeConverter.cs           # 文件大小显示转换器
       └── BoolToVisibilityConverter.cs   # 布尔值转可见性转换器
```

## 数据模型

### FileItem — 单个文件/文件夹

| 字段 | 类型 | 说明 |
|------|------|------|
| `Name` | string | 文件名 |
| `FullPath` | string | 完整路径 |
| `SizeBytes` | long | 文件大小（字节） |
| `Category` | string | 所属分类 |
| `SourceApp` | string | 产生文件的软件名称 |
| `SafetyTag` | SafetyTag | 安全等级 |
| `IsSelected` | bool | 是否勾选删除 |
| `IsDirectory` | bool | 是否为文件夹 |
| `Children` | ObservableCollection\<FileItem\> | 子项（树状结构） |

### ScanResult — 扫描结果

| 字段 | 类型 | 说明 |
|------|------|------|
| `Categories` | ObservableCollection\<FileCategory\> | 按分类分组的根节点 |
| `TotalSize` | long | 总计可清理大小 |
| `TotalFiles` | int | 文件总数 |

### SafetyTag 枚举

- `Safe`（🟢 绿色）— 可安全删除
- `Warning`（🟡 黄色）— 删除可能影响软件设置或加速效果
- `Danger`（🔴 红色）— 删除可能导致软件异常

### ScanTarget — 扫描配置

| 字段 | 类型 | 说明 |
|------|------|------|
| `SelectedDrives` | List\<string\> | 选中的盘符（如 `C:\`, `D:\`） |
| `CustomPaths` | List\<string\> | 用户添加的自定义路径 |
| `ScannerEnabled` | Dictionary\<string, bool\> | 各扫描器类型是否启用 |

### ExclusionEntry — 排除项

| 字段 | 类型 | 说明 |
|------|------|------|
| `Pattern` | string | 排除路径或模式 |
| `Description` | string | 用户备注 |

### CleanupHistoryRecord — 清理历史

| 字段 | 类型 | 说明 |
|------|------|------|
| `Timestamp` | DateTime | 清理时间 |
| `FilesDeleted` | int | 删除文件数 |
| `SizeFreed` | long | 释放空间（字节） |
| `ScannerSources` | List\<string\> | 涉及哪些扫描类型 |

## UI 布局

主窗口采用左侧分类树 + 右侧文件列表的经典布局：

```
┌─────────────────────────────────────────────────────────┐
│  WinClear - 系统垃圾清理                    [—] [×]      │
├─────────────────────────────────────────────────────────┤
│ [🔍 扫描] [⚡ 快速扫描] | [📋 扫描设置] [🚫 排除列表]   │  ← 新增快速扫描/设置/排除
│ [☑ 全选] [☒ 反选] | [🗑️ 删除选中]                      │
├──────────┬──────────────────────────────────────────────┤
│ 扫描结果  │  ┌──────────────────────────────────────┐   │
│          │  │ 总计: 2.3GB · 1,245 项               │   │
│ 📁 系统缓存│  ├──────────────────────────────────────┤   │
│   📁 临时  │  │ ☑ temp_cache.dat           45MB 🟢  │   │
│     ☐ f1   │  │ ☑ wechat_cache/*         1.2GB 🟡  │   │
│   📁 日志  │  │ ☐ chrome_cache/*         680MB 🟢  │   │
│ 📁 浏览器  │  │ ...                               │   │
│ 📁 应用缓存│  └──────────────────────────────────────┘   │
│ 📁 更新缓存│                                             │
│ 📁 大文件  │                                             │
│ 📁 重复文件│                                             │
│ 📁 隐私痕迹│                                             │
│ 📁 系统瘦身│                                             │
├──────────┴──────────────────────────────────────────────┤
│ 状态: 就绪  | 累计清理: 12.3GB  🟢 🟡 🔴                │  ← 新增清理历史
└─────────────────────────────────────────────────────────┘
```

### 交互逻辑

- 点击左侧分类节点，右侧显示该分类下的文件列表（树状结构）
- 每个文件前有 checkbox，支持勾选/取消
- "全选" / "反选" 按钮控制当前分类的所有文件
- 删除前弹出确认对话框，列出勾选文件的总大小和数量
- 右侧文件行显示 SafetyTag 图标（🟢🟡🔴）

## 核心扫描引擎工作流

```
用户点击 [⚡快速扫描] 或 [🔍扫描]
      ↓
有 ScanTarget 配置？ → 否 → 使用默认配置（全开 + 所有固定盘）
      ↓
ScanEngine.RunAsync(ScanTarget)
      ├─ 读取 ExclusionManager 排除列表
      ├─ 按 ScanTarget.ScannerEnabled 过滤要运行的扫描器
      ├─ 并行调度启用的 IScanner，传入 TargetPaths
      │   ├─ SystemTempScanner     → %TEMP%、Windows\Temp
      │   ├─ BrowserCacheScanner   → Chrome/Edge 缓存
      │   ├─ AppCacheScanner       → 微信/QQ 缓存
      │   ├─ WindowsUpdateScanner  → SoftwareDistribution\Download
      │   ├─ LargeFileScanner      → 指定盘符，>100MB 文件
      │   ├─ DuplicateFileScanner  → 指定盘符，MD5 重复检测
      │   ├─ UserDefinedScanner    → 用户自定义路径
      │   ├─ PrivacyTracesScanner  → 最近文档、快速访问历史
      │   └─ SystemSlimScanner     → 旧帮助文件、壁纸、示例媒体
      ↓
ExclusionManager.IsExcluded(path) 过滤 → 跳过匹配路径
      ↓
SafetyClassifier.Analyze(fileItem) → 根据内置规则表判断安全等级
      ↓
ScanEngine.MergeResults()
      ├─ 按 SourceApp 分组归类
      ├─ 构建树状 FileItem 层级
      └─ 绑定到 UI 的 ScanResult
      ↓
删除操作完成后 →
CleanupHistoryService.Record(时间, 文件数, 空间, 类型)
```

## 安全分类器规则

| 匹配规则 | SafetyTag | 说明 |
|----------|-----------|------|
| `%TEMP%` 下所有文件 | 🟢 Safe | 临时文件可安全删除 |
| 浏览器缓存文件 | 🟢 Safe | 删除后浏览器重新缓存 |
| 回收站文件 | 🟢 Safe | 彻底删除 |
| Windows 更新备份 (.bak) | 🟢 Safe | 可清理已安装更新的备份 |
| 应用缓存（微信/QQ图片等） | 🟡 Warning | 删除后需重新下载 |
| 缩略图缓存 (Thumbs.db) | 🟢 Safe | 会自动重建 |
| `.exe` / `.dll` / `.sys` | 🔴 Danger | 可能导致程序无法运行 |
| 注册表备份 | 🟡 Warning | 系统还原点相关 |
| 用户自定义路径 | 🟢 Safe（默认） | 用户自行添加的路径 |
| 大文件（用户文档等） | 🟡 Warning | 用户自行判断 |

用户可在结果中右键手动调整任一文件的 SafetyTag。

## 删除流程与安全机制

```
用户勾选文件 → 点击 [删除选中]
      ↓
确认对话框（文件数、总计大小、⚠️ 文件数）
      ↓
用户确认 → 逐文件删除（异常捕获）
      ↓
显示清理报告（成功数、失败数、查看详情）
```

### 保护措施
- 删除前可创建系统还原点（可选，默认关闭）
- 默认移入回收站 vs 直接删除（按住 Shift）
- 🔴 Danger 文件始终高亮提示并要求二次确认
- 删除失败的文件记录到日志

## 大文件扫描

- 遍历 C 盘（可配置），筛选 >100MB 的文件
- 按大小降序排列
- 默认 🟡 Warning 标签

## 重复文件检测

- 先按文件大小分组
- 同组内计算 MD5 哈希对比
- 保留一个原文件，其余标记为可删除（默认 🟡 Warning）
- 首次扫描限制目录深度/数量以保证性能

## 扫描结果中文件归类

扫描完成后，所有文件按以下方式自动归类并展示为树状列表：

- **系统临时文件** — 由 SystemTempScanner 产生，归入 "系统缓存" 分类
- **浏览器缓存** — 由 BrowserCacheScanner 产生，按浏览器名称（Chrome/Edge）分组
- **应用缓存** — 由 AppCacheScanner 产生，按应用名称（微信/QQ）分组
- **Windows 更新缓存** — 由 WindowsUpdateScanner 产生
- **大文件** — 由 LargeFileScanner 产生
- **重复文件** — 由 DuplicateFileScanner 产生
- **隐私痕迹** — 由 PrivacyTracesScanner 产生
- **系统瘦身** — 由 SystemSlimScanner 产生

每个分类节点显示该分类的总计大小和文件数，方便用户判断。

## 一键快速扫描

工具栏新增 "⚡快速扫描" 按钮，使用默认配置：
- 所有扫描器类型启用
- 所有固定驱动器选中
- 无自定义路径
- 直接执行 ScanEngine.RunAsync(默认ScanTarget)
- 适合日常快速清理，无需进入设置界面

## 扫描设置弹窗

新增 ScanSettingsWindow，在 "📋扫描设置" 按钮打开：

```
┌─────────────────────────────────────┐
│  WinClear - 扫描设置                │
├─────────────────────────────────────┤
│ 📁 选择扫描位置                     │
│                                     │
│  ☑ C:\ (系统)      [浏览文件夹...]  │
│  ☐ D:\ (数据)      [ + 添加路径 ]  │
│  ── 已添加的自定义路径 ──           │
│  ☑ D:\MyProjects    [× 移除]       │
│                                     │
│ 📋 选择扫描类型                     │
│                                     │
│  ☑ 系统临时文件  ☑ 浏览器缓存      │
│  ☑ 应用缓存      ☑ 更新缓存        │
│  ☑ 大文件        ☑ 重复文件        │
│  ☑ 隐私痕迹      ☑ 系统瘦身        │
│                                     │
│           [取消]  [开始扫描]        │
└─────────────────────────────────────┘
```

## 隐私痕迹清理

PrivacyTracesScanner 扫描以下系统隐私痕迹：

| 扫描目标 | 路径 | SafetyTag |
|----------|------|-----------|
| 最近文档 | `%APPDATA%\Microsoft\Windows\Recent` | 🟢 Safe |
| 快速访问历史 | `%APPDATA%\Microsoft\Windows\Recent\AutomaticDestinations` | 🟢 Safe |
| 跳转列表 | `%APPDATA%\Microsoft\Windows\Recent\CustomDestinations` | 🟢 Safe |

## 系统瘦身

SystemSlimScanner 扫描以下可清理的 Windows 系统文件：

| 扫描目标 | 路径 | SafetyTag |
|----------|------|-----------|
| Windows 旧帮助文件 | `%WINDIR%\Help` | 🟡 Warning |
| 系统自带壁纸(保留一张) | `%WINDIR%\Web\Wallpaper` | 🟡 Warning |
| 示例媒体文件 | `%WINDIR%\System32\oobe\info\backgrounds` | 🟢 Safe |
| 旧版 Windows 备份 | `C:\Windows.old`（如存在） | 🔴 Danger |
| 输入法缓存/无关文件 | 各 IME 缓存目录 | 🟡 Warning |

## 排除列表（白名单）

ExclusionManager 管理用户不希望被删除的文件/路径：
- 持久化到 `%LOCALAPPDATA%\WinCleaner\exclusions.json`
- 提供 `IsExcluded(string filePath): bool` 扫描时实时过滤
- 支持路径前缀匹配
- UI: ExclusionListWindow 弹窗管理列表（添加/移除）

## 清理历史

CleanupHistoryService 记录每次清理操作：
- 持久化到 `%LOCALAPPDATA%\WinCleaner\history.json`
- 保留最近 100 条记录
- 状态栏显示 "累计清理: XX.XGB"
- 每完成一次删除操作自动记录

## 待办/未来扩展

- [ ] 定时清理计划任务
- [ ] 多语言支持
- [ ] 导出清理报告
