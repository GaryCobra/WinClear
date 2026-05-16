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
}
