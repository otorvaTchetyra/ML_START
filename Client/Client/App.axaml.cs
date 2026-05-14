using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Client.Data;
using Client.Services;
using Client.ViewModels;
using Client.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using Splat;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    public partial class App : Application
    {
        public static Window? MainWindow { get; private set; }
        private IServiceProvider _serviceProvider = null!;

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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                    throw;
                }
            }

            base.OnFrameworkInitializationCompleted();
            try
            {
                var navigationService = _serviceProvider?.GetRequiredService<NavigationService>();
                _ = navigationService?.NavigateToLoginAsync();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var journalService = _serviceProvider?.GetRequiredService<JournalService>();
                        if (journalService != null)
                        {
                            await journalService.RecordAsync(
                                eventCode: "app_start",
                                message: "Приложение клиента запущено",
                                source: "system",
                                action: "startup",
                                level: "info");
                        }
                    }
                    catch
                    {
                        // Журнал не должен мешать запуску приложения.
                    }
                });
            }
            catch
            {
                throw new NotImplementedException();
            }
        }
        private IServiceProvider CreateContainer()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            ApplyTheme(configuration["Theme"] ?? "Dark");

            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IConfigurationService, ConfigurationService>();

           
            services.AddSingleton<HttpClient>(provider =>
            {
                var configService = provider.GetRequiredService<IConfigurationService>();
                return new HttpClient
                {
                    BaseAddress = new Uri(configService.GetApiUrl()),
                    Timeout = Timeout.InfiniteTimeSpan
                };
            });

            
            services.AddSingleton<IApiClient>(provider =>
            {
                var httpClient = provider.GetRequiredService<HttpClient>();
                return new ApiClient(httpClient);
            });

            var journalConnectionString =
                configuration["JournalDatabase:ConnectionString"]
                ?? "server=localhost;port=3307;database=dronevision_client;user=root;password=rootpassword";

            services.AddDbContextFactory<JournalDbContext>(options =>
                options.UseMySql(journalConnectionString, new MySqlServerVersion(new Version(8, 0, 36))));

            services.AddScoped<AuthService>();
            services.AddScoped<NavigationService>();
            services.AddScoped<RoutableViewModelsFactory>();
            services.AddScoped<MainWindowViewModel>();

           
            services.AddTransient<MainViewModel>();

            services.AddTransient<LoginViewModel>();
            services.AddTransient<RegisterViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<StreamViewModel>();
            services.AddTransient<AdminHomeViewModel>();
            services.AddTransient<AdminUsersViewModel>();
            services.AddTransient<AdminJournalViewModel>();

            services.AddScoped<IHealthService, HealthService>();

            services.AddScoped<IScreen>(provider =>
                provider.GetRequiredService<MainWindowViewModel>());

            services.AddScoped<EventsService>();
            services.AddScoped<StreamService>();
            services.AddScoped<StatsService>();
            services.AddScoped<LogsService>();
            services.AddScoped<UsersService>();
            services.AddScoped<JournalService>();
            services.AddScoped<CameraCaptureService>();

            return services.BuildServiceProvider();
        }

        private static void RegisterLocatorComponents()
        {
            Locator.CurrentMutable.Register<IViewFor<RegisterViewModel>>(() => new RegisterView());
            Locator.CurrentMutable.Register<IViewFor<LoginViewModel>>(() => new LoginView());
            Locator.CurrentMutable.Register<IViewFor<SettingsViewModel>>(() => new SettingsView());
            Locator.CurrentMutable.Register<IViewFor<MainViewModel>>(() => new MainView());
            Locator.CurrentMutable.Register<IViewFor<StreamViewModel>>(() => new StreamView());
            Locator.CurrentMutable.Register<IViewFor<AdminHomeViewModel>>(() => new AdminHomeView());
            Locator.CurrentMutable.Register<IViewFor<AdminUsersViewModel>>(() => new AdminUsersView());
            Locator.CurrentMutable.Register<IViewFor<AdminJournalViewModel>>(() => new AdminJournalView());
        }

        public static void ApplyTheme(string theme)
        {
            if (Application.Current is not App app)
                return;

            var isLight = string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase);
            app.RequestedThemeVariant = isLight ? ThemeVariant.Light : ThemeVariant.Dark;

            app.Resources["PageBackgroundBrush"] = new SolidColorBrush(Color.Parse(isLight ? "#F3F6FA" : "#090B10"));
            app.Resources["PanelBrush"] = new SolidColorBrush(Color.Parse(isLight ? "#FFFFFF" : "#11161D"));
            app.Resources["FieldBrush"] = new SolidColorBrush(Color.Parse(isLight ? "#F7F9FC" : "#0D1117"));
            app.Resources["BorderBrush"] = new SolidColorBrush(Color.Parse(isLight ? "#D4DEEA" : "#243041"));
            app.Resources["AccentBrush"] = new SolidColorBrush(Color.Parse("#2AA7FF"));
            app.Resources["TextPrimaryBrush"] = new SolidColorBrush(Color.Parse(isLight ? "#15202B" : "#F5F7FA"));
            app.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.Parse(isLight ? "#5D6C7B" : "#9AA7B5"));
            app.Resources["MutedBrush"] = new SolidColorBrush(Color.Parse(isLight ? "#8090A0" : "#6F7A86"));
            app.Resources["DangerBrush"] = new SolidColorBrush(Color.Parse("#FF6B6B"));
        }
    }
}
