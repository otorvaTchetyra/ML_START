using Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;

namespace Client.Services;

public class RoutableViewModelsFactory(IServiceProvider serviceProvider)
{
    private IServiceProvider _serviceProvider = serviceProvider;

    public LoginViewModel CreateLoginViewModel() => _serviceProvider.GetRequiredService<LoginViewModel>();
    public RegisterViewModel CreateRegisterViewModel() => _serviceProvider.GetRequiredService<RegisterViewModel>();
    public SettingsViewModel CreateSettingsViewModel() => _serviceProvider.GetRequiredService<SettingsViewModel>();
    public MainViewModel CreateMainViewModel() => _serviceProvider.GetRequiredService<MainViewModel>();
    public StatisticsViewModel CreateStatisticsViewModel() => _serviceProvider.GetRequiredService<StatisticsViewModel>();
    public StreamViewModel CreateStreamViewModel() => _serviceProvider.GetRequiredService<StreamViewModel>();
    public AdminHomeViewModel CreateAdminHomeViewModel() => _serviceProvider.GetRequiredService<AdminHomeViewModel>();
    public AdminUsersViewModel CreateAdminUsersViewModel() => _serviceProvider.GetRequiredService<AdminUsersViewModel>();
    public AdminJournalViewModel CreateAdminJournalViewModel() => _serviceProvider.GetRequiredService<AdminJournalViewModel>();
}
