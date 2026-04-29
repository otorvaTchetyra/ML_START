using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Client.Services;
using Client.ViewModels;
using Client.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Splat;
using System;
using System.Linq;
using System.Net.Http;

namespace Client
{
    public partial class App : Application
    {
        public static Window? MainWindow { get; private set; }
        private IServiceProvider _serviceProvider;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                RegisterLocatorComponents();
                _serviceProvider = CreateContainer();
                try
                {
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = _serviceProvider?.GetRequiredService<MainWindowViewModel>(),
                    };
                MainWindow = desktop.MainWindow;
                }
                catch
                {
                    throw new NotImplementedException();
                }
            }

            base.OnFrameworkInitializationCompleted();
            try
            {
                var navigationService = _serviceProvider?.GetRequiredService<NavigationService>();
                _ = navigationService?.NavigateToLoginAsync();
            }
            catch
            {
                throw new NotImplementedException();
            }
        }
        private IServiceProvider CreateContainer()
        {
            IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<IApiClient>(provider =>
            {
                var configService = provider.GetRequiredService<IConfigurationService>();
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(configService.GetApiUrl()),
                    Timeout = TimeSpan.FromSeconds(30)
                };
                return new ApiClient(httpClient);
            });
            services.AddScoped<AuthService>();
            services.AddScoped<NavigationService>();
            services.AddScoped<RoutableViewModelsFactory>();
            services.AddScoped<MainWindowViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<RegisterViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddScoped<IHealthService, HealthService>();
            try
            {
                services.AddScoped<IScreen>(provider => provider?.GetRequiredService<MainWindowViewModel>());
            }
            catch
            {
                throw new NotImplementedException();
            }
            return services.BuildServiceProvider();
        }

        private static void RegisterLocatorComponents()
        {
            Locator.CurrentMutable.Register<IViewFor<RegisterViewModel>>(() => new RegisterView());
            Locator.CurrentMutable.Register<IViewFor<LoginViewModel>>(() => new LoginView());
            //Locator.CurrentMutable.Register<IViewFor<SettingsViewModel>>(() => new SettingsView());
        }
    }
}