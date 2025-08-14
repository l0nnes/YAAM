using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using YAAM.App.Services;
using YAAM.Core;
using YAAM.Core.Models;

namespace YAAM.App.ViewModels;

/// <summary>
/// The main ViewModel for the application. It orchestrates the UI logic.
/// </summary>
public partial class MainViewModel(AutostartManager autostartManager) : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<AutostartItem> _items = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;
    
    public IDialogService? DialogService { get; set; }
    
    public bool IsNotBusy => !_isBusy;

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        Items.Clear();

        try
        {
            var autostartItems = await autostartManager.GetAllItemsAsync();
            autostartItems.ForEach(item => Items.Add(item));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading autostart items: {ex.Message}");
            //TODO: Show error message
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleEnableStateAsync(AutostartItem? autostartItem)
    {
        if (autostartItem == null || IsBusy) return;
        
        try
        {
            await autostartManager.ToggleEnableStateAsync(autostartItem);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error toggling autostart state: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteItemAsync(AutostartItem? autostartItem)
    {
        if (autostartItem == null || IsBusy) return;

        IsBusy = true;
        try
        {
            await autostartManager.DeleteItemAsync(autostartItem);
            Items.Remove(autostartItem);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting autostart item: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    [RelayCommand]
    private async Task CreateItemAsync()
    {
        if (DialogService == null || IsBusy) return;

        var newItem = DialogService.ShowAddItemDialog();
        
        if (newItem == null) return;
        
        IsBusy = true;
        try
        {
            await autostartManager.CreateItemAsync(newItem);
            Items.Add(newItem);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating autostart item: {ex.Message}");
            // Show error to the user
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ModifyItemAsync(Tuple<AutostartItem, AutostartItem>? data)
    {
        // This command assumes 'data' is provided, where Item1 is the original ViewModel
        // and Item2 is the new, modified data model from a dialog.
        if (data?.Item1 == null || data?.Item2 == null || IsBusy) return;

        var originalItem = data.Item1;
        var modifiedItem = data.Item2;

        IsBusy = true;
        try
        {
            // Call the business logic with the original model and the modified model.
            await autostartManager.ModifyItemAsync(originalItem, modifiedItem);

            // If successful, update the ViewModel in place to refresh the UI.
            // This now works because AutostartItemViewModel has setters.
            originalItem.Name = modifiedItem.Name;
            originalItem.ExecutablePath = modifiedItem.ExecutablePath;
            originalItem.Arguments = modifiedItem.Arguments;
            
            // We also need to update the underlying model of the ViewModel
            // so that subsequent operations have the correct data.
            originalItem.Name = modifiedItem.Name;
            originalItem.ExecutablePath = modifiedItem.ExecutablePath;
            originalItem.Arguments = modifiedItem.Arguments;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error modifying autostart item: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}