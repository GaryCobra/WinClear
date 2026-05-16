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
- `dotnet build`
- `dotnet run --project WinClear/WinClear`
