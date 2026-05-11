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
    public async Task NavigateToSettingsAsync()
    {
        var settingsViewModel = _routableViewModelsFactory.CreateSettingsViewModel();
        await settingsViewModel.InitializeAsync();
        await _screen.Router.Navigate.Execute(settingsViewModel);
    }
    public async Task NavigateToMainAsync()
    {
        var mainViewModel = _routableViewModelsFactory.CreateMainViewModel();
        await mainViewModel.InitializeAsync();
        await _screen.Router.Navigate.Execute(mainViewModel);
    }
    public async Task NavigateToStreamAsync()
    {
        var mainViewModel = _routableViewModelsFactory.CreateStreamViewModel();
        await mainViewModel.InitializeAsync();
        await _screen.Router.Navigate.Execute(mainViewModel);
    }
}
