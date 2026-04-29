using Client.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;

namespace Client.ViewModels
{
	public class SettingsViewModel : ReactiveObject, IRoutableViewModel
    {
        private IConfigurationService _configurationService;
        private IHealthService _healthService;
        private NavigationService _navigationService;
        //public IReadOnlyList<UserModel> Users = new List<UserModel>();

        private bool IsInitialized;

        public string? UrlPathSegment => "settings";
        public IScreen HostScreen { get; }
        public string UrlServerPath {
            get;
            set => _configurationService.SaveApiUrl(value);
        }

        public ReactiveCommand<Unit, Task> NavigateToMainCommand { get; }
        public ReactiveCommand<Unit, Task> DropDBCommand { get; }
        public SettingsViewModel( IConfigurationService configurationService, IHealthService healthService, IScreen screen, NavigationService navigationService) 
		{
			_configurationService = configurationService;
            _navigationService = navigationService;
            _healthService = healthService;
            HostScreen = screen;

            NavigateToMainCommand = ReactiveCommand.Create(NavigateToMainAsync);
            DropDBCommand = ReactiveCommand.Create(DropDBAsync);
        }

       

        private async Task NavigateToMainAsync()
        {
            await _navigationService.NavigateToMainAsync();
        }
        private async Task DropDBAsync()
        {
            await _healthService.DropDBAsync();
        }
        public async Task InitializeAsync()
        {
            if (!IsInitialized)
            {
                await LoadData();
                IsInitialized = true;
            }
        }

        private async Task LoadData()
        {
        }


    }
}