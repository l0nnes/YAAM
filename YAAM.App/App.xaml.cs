using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using YAAM.App.ViewModels;
using YAAM.Core;
using YAAM.Core.Interfaces;
using YAAM.Core.Services;

namespace YAAM.App;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IAutostartProvider, RegistryAutostartProvider>();
        services.AddSingleton<IAutostartProvider, ScheduledTaskAutostartProvider>();
        services.AddSingleton<IAutostartProvider, ServiceAutostartProvider>();

        services.AddSingleton<AutostartManager>();
        
        services.AddSingleton<AddItemViewModel>();
        services.AddSingleton<AddItemWindow>();
        
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var mainWindow = _serviceProvider.GetService<MainWindow>();
        
        mainWindow?.Show();
    }
}