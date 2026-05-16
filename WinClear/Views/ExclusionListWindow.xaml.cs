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
