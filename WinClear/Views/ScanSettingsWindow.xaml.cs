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

    private void StartScan_Click(object sender, RoutedEventArgs e)
    {
        Result = ViewModel.BuildScanTarget();
        DialogResult = true;
        Close();
    }
}
