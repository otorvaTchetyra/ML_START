using ReactiveUI;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Client.Services;

public sealed class NavigationService
{
    private RoutableViewModelsFactory _routableViewModelsFactory;
    private IScreen _screen;

    public NavigationService(RoutableViewModelsFactory routableViewModelsFactory, IScreen screen)
    {
        _routableViewModelsFactory = routableViewModelsFactory;
        _screen = screen;
    }

    public async Task NavigateToLoginAsync()
    {
        var loginViewModel = _routableViewModelsFactory.CreateLoginViewModel();
        await loginViewModel.InitializeAsync();
        await _screen.Router.Navigate.Execute(loginViewModel);
    }

    public async Task NavigateToRegisterAsync()
    {
        var registerViewModel = _routableViewModelsFactory.CreateRegisterViewModel();
        await registerViewModel.InitializeAsync();
        await _screen.Router.Navigate.Execute(registerViewModel);
    }

    public async Task NavigateToAdminHomeAsync()
    {
        var vm = _routableViewModelsFactory.CreateAdminHomeViewModel();
        await vm.InitializeAsync();
        await _screen.Router.Navigate.Execute(vm);
    }

    public async Task NavigateToAdminUsersAsync()
    {
        var vm = _routableViewModelsFactory.CreateAdminUsersViewModel();
        await vm.InitializeAsync();
        await _screen.Router.Navigate.Execute(vm);
    }

    public async Task NavigateToAdminJournalAsync()
    {
        var vm = _routableViewModelsFactory.CreateAdminJournalViewModel();
        await vm.InitializeAsync();
        await _screen.Router.Navigate.Execute(vm);
    }

    public async Task NavigateToSettingsAsync()
    {
        var settingsViewModel = _routableViewModelsFactory.CreateSettingsViewModel();
        await settingsViewModel.InitializeAsync();
        await _screen.Router.Navigate.Execute(settingsViewModel);
    }

    public async Task NavigateToMainAsync()
    {
        var mainViewModel = _routableViewModelsFactory.CreateMainViewModel();
        await _screen.Router.Navigate.Execute(mainViewModel);
        _ = mainViewModel.InitializeAsync();
    }

    public async Task NavigateToStreamAsync()
    {
        var mainViewModel = _routableViewModelsFactory.CreateStreamViewModel();
        await mainViewModel.InitializeAsync();
        await _screen.Router.Navigate.Execute(mainViewModel);
    }
}
