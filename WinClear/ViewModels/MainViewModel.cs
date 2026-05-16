using System.Collections.ObjectModel;
using System.IO;
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
    private ObservableCollection<FileItem>? _currentFileItems;

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

            StatusText = $"扫描完成 - 共 {result.TotalFiles} 项，{Helpers.FileSizeFormatter.Format(result.TotalSize)}";
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

        var message = $"确定要删除选中的 {toDelete.Count} 个文件 ({Helpers.FileSizeFormatter.Format(totalSize)})？";
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

    private bool CanDeleteSelected() => SelectedCount > 0;

    private void SetAllSelection(IEnumerable<FileItem> items, bool selected)
    {
        foreach (var item in items)
        {
            item.IsSelected = selected;
            if (item.Children.Count > 0)
                SetAllSelection(item.Children, selected);
        }
    }

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
