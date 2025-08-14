using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using YAAM.Core.Models;

namespace YAAM.App.ViewModels;

public partial class AddItemViewModel : ObservableObject
{
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = "";

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _executablePath = "";

    [ObservableProperty] private string? _arguments;

    [ObservableProperty] private bool _isEnabled = true;

    [ObservableProperty] private AutostartType _selectedType = AutostartType.Registry;

    [ObservableProperty] private string _selectedRegistryScope = "Current User";

    public ObservableCollection<AutostartType> AutostartTypes { get; } = new(Enum.GetValues<AutostartType>());
    public ObservableCollection<string> RegistryScopes { get; } = ["Current User", "All Users (Local Machine)"];
    public AutostartItem? ResultItem { get; private set; }

    public event Action<bool> SetDialogResult;

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(ExecutablePath) &&
               File.Exists(ExecutablePath);
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        var location = "";
        switch (SelectedType)
        {
            case AutostartType.Registry:
                var scope = SelectedRegistryScope == "Current User" ? "CurrentUser" : "LocalMachine";
                location = @$"{scope}\Software\Microsoft\Windows\CurrentVersion\Run";
                break;
            case AutostartType.ScheduledTask:
                // For tasks, the location is the full path, which is the name for root tasks.
                location = @$"\{Name}";
                break;
            case AutostartType.ThirdPartyService:
                // For services, the location is the system service name. We'll use the Name for this.
                location = Name;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        ResultItem = new AutostartItem
        {
            Name = Name,
            ExecutablePath = ExecutablePath,
            Arguments = Arguments,
            IsEnabled = IsEnabled,
            Type = SelectedType,
            Location = location
        };
        
        // Close the dialog with a success result
        SetDialogResult.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        // Close the dialog with a failure result
        SetDialogResult?.Invoke(false);
    }

    [RelayCommand]
    private void Browse()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            Title = "Select an executable"
        };

        if (dialog.ShowDialog() == true)
        {
            ExecutablePath = dialog.FileName;
        }
    }
}