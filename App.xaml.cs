using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SineParameterTrainer.Services;
using SineParameterTrainer.ViewModels;

namespace SineParameterTrainer;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISineCurveService, SineCurveService>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
        _serviceProvider = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
