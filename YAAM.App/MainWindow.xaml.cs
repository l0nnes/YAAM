using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;
using YAAM.App.Services;
using YAAM.App.ViewModels;
using YAAM.Core.Models;

namespace YAAM.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : IDialogService
{
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        DataContext = viewModel;
        _serviceProvider = serviceProvider;
        
        // Pass the service implementation to the ViewModel
        viewModel.DialogService = this; 
    }

    public AutostartItem? ShowAddItemDialog()
    {
        // Use the DI container to create the window and its viewmodel
        var viewModel = _serviceProvider.GetRequiredService<AddItemViewModel>();
        var dialogWindow = _serviceProvider.GetRequiredService<AddItemWindow>();
        dialogWindow.DataContext = viewModel; // Ensure correct DataContext
        dialogWindow.Owner = this; // Set owner for proper modal behavior

        var result = dialogWindow.ShowDialog();

        return result == true ? viewModel.ResultItem : null;
    }
}