using System.Diagnostics;
using System.Windows;
using YAAM.App.ViewModels;

namespace YAAM.App;

public partial class AddItemWindow : Window
{
    public AddItemWindow(AddItemViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        viewModel.SetDialogResult += OnSubmit;
    }

    private void OnSubmit(bool result)
    {
        DialogResult = result;
        Close();
    }
}