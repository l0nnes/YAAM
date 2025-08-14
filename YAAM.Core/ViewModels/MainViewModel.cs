using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using YAAM.Core.Interfaces;
using YAAM.Core.Models;

namespace YAAM.Core.ViewModels;

public partial class MainViewModel(IEnumerable<IAutostartProvider> providers) : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<AutostartItem> _items = [];
    
    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        Items.Clear();
        
        var allTasks = providers.Select(p => p.GetAutostartItemsAsync());
        var results = await Task.WhenAll(allTasks);

        foreach (var result in results)
        {
            foreach (var item in result)
            {
                Items.Add(item);
            }
        }
    }

    [RelayCommand]
    private async Task ToggleEnableStateAsync(AutostartItem item)
    {
        var provider = providers.FirstOrDefault(p => p.Type == item.Type);
        if (provider == null) return;

        try
        {
            if (item.IsEnabled)
            {
                await provider.DisableAutostartItem(item);
            }
            else
            {
                await provider.EnableAutostartItem(item);
            }
            
            item.IsEnabled = !item.IsEnabled;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error toggling autostart state: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task CreateItemAsync(AutostartItem? newItem)
    {
        if (newItem == null) return;

        var provider = providers.FirstOrDefault(p => p.Type == newItem.Type);
        if (provider == null) return;

        try
        {
            await provider.CreateAutostartItem(newItem);
            Items.Add(newItem);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating autostart item: {ex.Message}");
            throw;
        }
    }

    [RelayCommand]
    private async Task ModifyItemAsync(Tuple<AutostartItem, AutostartItem>? items)
    {
        if (items?.Item1 == null || items?.Item2 == null) return;

        var originalItem = items.Item1;
        var modifiedItem = items.Item2;

        var provider = providers.FirstOrDefault(p => p.Type == originalItem.Type);
        if (provider == null) return;

        try
        {
            await provider.ModifyAutostartItem(originalItem, modifiedItem);

            originalItem.Name = modifiedItem.Name;
            originalItem.ExecutablePath = modifiedItem.ExecutablePath;
            originalItem.Arguments = modifiedItem.Arguments;
            originalItem.Location = modifiedItem.Location;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error modifying autostart item: {ex.Message}");
            throw;
        }
    }

    [RelayCommand]
    private async Task DeleteItemAsync(AutostartItem? item)
    {
        if (item == null) return;

        var provider = providers.FirstOrDefault(p => p.Type == item.Type);
        if (provider == null) return;

        try
        {
            await provider.DeleteAutostartItem(item);
            Items.Remove(item);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting autostart item: {ex.Message}");
            throw;
        }
    }
}