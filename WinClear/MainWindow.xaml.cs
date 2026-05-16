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
