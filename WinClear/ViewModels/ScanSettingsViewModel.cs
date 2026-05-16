using System.Collections.ObjectModel;
using System.IO;
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
    private ObservableCollection<DriveItemViewModel> _drives = new();

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
                Drives.Add(new DriveItemViewModel
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
    private void StartScan()
    {
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

public partial class DriveItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}
