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
   ├─ App.xaml / App.xaml.cs             # WPF 入口
   ├─ MainWindow.xaml / MainWindow.xaml.cs
   ├─ Models/
   │   ├─ ScanResult.cs                  # 扫描结果聚合
   │   ├─ FileItem.cs                    # 文件/文件夹数据模型
   │   └─ SafetyTag.cs                   # 安全等级枚举
   ├─ ViewModels/
   │   ├─ MainViewModel.cs               # 主窗口 ViewModel
   │   └─ ScanResultViewModel.cs         # 扫描结果 ViewModel
   ├─ Services/
   │   ├── IScanner.cs                   # 扫描器接口
   │   ├── ScanEngine.cs                 # 扫描引擎
   │   └── SafetyClassifier.cs           # 安全等级分类器
   ├─ Services/ScannerProviders/
   │   ├── SystemTempScanner.cs          # 系统临时文件扫描
   │   ├── BrowserCacheScanner.cs        # 浏览器缓存扫描
   │   ├── AppCacheScanner.cs            # 应用缓存扫描
   │   ├── WindowsUpdateScanner.cs       # Windows 更新缓存扫描
   │   ├── LargeFileScanner.cs           # 大文件扫描
   │   ├── DuplicateFileScanner.cs       # 重复文件扫描
   │   └── UserDefinedScanner.cs         # 用户自定义路径扫描
   ├─ Helpers/
   │   └── FileSizeFormatter.cs          # 文件大小格式化
   └─ Converters/
       └── SafetyTagColorConverter.cs    # 安全等级颜色转换器
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

## UI 布局

主窗口采用左侧分类树 + 右侧文件列表的经典布局：

```
┌─────────────────────────────────────────────┐
│  WinClear - 系统垃圾清理          [设置] [—] [×] │
├──────────┬──────────────────────────────────┤
│ 扫描结果  │  ┌──────────────────────────┐   │
│          │  │ 总计: 2.3GB · 1,245 项   │   │
│ 📁 系统缓存│  ├──────────────────────────┤   │
│   📁 临时文件│  │ ☑ 文件/文件夹名      大小  ⚠️ │   │
│     ☐ file1  │  │ ☐ temp_cache.dat   45MB 🟢 │   │
│   📁 日志  │  │ ☑ wechat_cache/*    1.2GB 🟡 │   │
│ 📁 浏览器  │  │ ☐ chrome_cache/*   680MB 🟢 │   │
│   📁 Chrome │  │ ...                       │   │
│   📁 Edge  │  └──────────────────────────┘   │
│ 📁 应用缓存│                                    │
│ 📁 更新缓存│  [☑ 全选] [反选] [扫描] [删除选中]    │
│ 📁 大文件  │                                    │
│ 📁 重复文件│                                    │
├──────────┴──────────────────────────────────┤
│ 🟢 安全删除  🟡 可能影响  🔴 谨慎操作           │
└─────────────────────────────────────────────┘
```

### 交互逻辑

- 点击左侧分类节点，右侧显示该分类下的文件列表（树状结构）
- 每个文件前有 checkbox，支持勾选/取消
- "全选" / "反选" 按钮控制当前分类的所有文件
- 删除前弹出确认对话框，列出勾选文件的总大小和数量
- 右侧文件行显示 SafetyTag 图标（🟢🟡🔴）

## 核心扫描引擎工作流

```
用户点击 [扫描]
      ↓
ScanEngine.RunAsync()
      ├─ 并行调度所有 IScanner
      │   ├─ SystemTempScanner    → %TEMP%、回收站、Prefetch
      │   ├─ BrowserCacheScanner  → Chrome/Edge/Firefox 缓存
      │   ├─ AppCacheScanner      → 微信/QQ/抖音 等已知路径
      │   ├─ WindowsUpdateScanner → WinSxS 备份、SoftwareDistribution
      │   ├─ LargeFileScanner     → C 盘 >100MB 的文件
      │   ├─ DuplicateFileScanner → MD5 哈希检测重复文件
      │   └─ UserDefinedScanner  → 用户自定义路径
      ↓
SafetyClassifier.Analyze(fileItem)
      ├─ 根据内置规则表判断安全等级
      ↓
ScanEngine.MergeResults()
      ├─ 按 SourceApp 分组归类
      ├─ 构建树状 FileItem 层级
      └─ 绑定到 UI 的 ScanResult
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
- **浏览器缓存** — 由 BrowserCacheScanner 产生，按浏览器名称（Chrome/Edge/Firefox）分组
- **应用缓存** — 由 AppCacheScanner 产生，按应用名称（微信/QQ/抖音等）分组
- **Windows 更新缓存** — 由 WindowsUpdateScanner 产生
- **大文件** — 由 LargeFileScanner 产生
- **重复文件** — 由 DuplicateFileScanner 产生

每个分类节点显示该分类的总计大小和文件数，方便用户判断。

## 待办/未来扩展

- [ ] 白名单功能：可设置永不删除的文件/路径
- [ ] 定时清理计划任务
- [ ] 清理历史记录统计
- [ ] 多语言支持
- [ ] 导出清理报告
