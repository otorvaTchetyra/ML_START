using Avalonia.Media;
using Client.Models;
using Client.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Client.ViewModels;

public class AdminUsersViewModel : ViewModelBase, IRoutableViewModel
{
    private bool _isInitialized;

    public string? UrlPathSegment => "admin-users";
    public IScreen HostScreen { get; }

    private readonly UsersService _usersService;
    private readonly AuthService _authService;
    private readonly JournalService _journalService;
    private readonly NavigationService _navigationService;

    private ObservableCollection<ApiUser> _serverUsers = new();
    private ApiUser? _selectedServerUser;
    private string _newOperatorUsername = string.Empty;
    private string _newOperatorPassword = string.Empty;
    private string _editUsername = string.Empty;
    private string _editPassword = string.Empty;
    private string _editRole = "operator";
    private string _feedbackMessage = string.Empty;
    private IBrush _feedbackBrush = Brushes.Transparent;
    private bool _isBusy;

    public ObservableCollection<ApiUser> ServerUsers
    {
        get => _serverUsers;
        set => this.RaiseAndSetIfChanged(ref _serverUsers, value);
    }

    public ApiUser? SelectedServerUser
    {
        get => _selectedServerUser;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedServerUser, value);
            if (value != null)
            {
                EditUsername = value.Username;
                EditRole = value.Role;
                EditPassword = string.Empty;
            }
            this.RaisePropertyChanged(nameof(IsEditVisible));
        }
    }

    public bool IsEditVisible => _selectedServerUser != null;

    public string NewOperatorUsername
    {
        get => _newOperatorUsername;
        set => this.RaiseAndSetIfChanged(ref _newOperatorUsername, value);
    }

    public string NewOperatorPassword
    {
        get => _newOperatorPassword;
        set => this.RaiseAndSetIfChanged(ref _newOperatorPassword, value);
    }

    public string EditUsername
    {
        get => _editUsername;
        set => this.RaiseAndSetIfChanged(ref _editUsername, value);
    }

    public string EditPassword
    {
        get => _editPassword;
        set => this.RaiseAndSetIfChanged(ref _editPassword, value);
    }

    public string EditRole
    {
        get => _editRole;
        set => this.RaiseAndSetIfChanged(ref _editRole, value);
    }

    public string[] AvailableRoles { get; } = ["operator", "admin"];

    public string FeedbackMessage
    {
        get => _feedbackMessage;
        set
        {
            this.RaiseAndSetIfChanged(ref _feedbackMessage, value);
            this.RaisePropertyChanged(nameof(HasFeedback));
        }
    }

    public IBrush FeedbackBrush
    {
        get => _feedbackBrush;
        set => this.RaiseAndSetIfChanged(ref _feedbackBrush, value);
    }

    public bool HasFeedback => !string.IsNullOrWhiteSpace(FeedbackMessage);

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public ReactiveCommand<Unit, Unit> BackCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateOperatorCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteUserCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveEditCommand { get; }

    public AdminUsersViewModel(
        IScreen screen,
        UsersService usersService,
        AuthService authService,
        JournalService journalService,
        NavigationService navigationService)
    {
        HostScreen = screen;
        _usersService = usersService;
        _authService = authService;
        _journalService = journalService;
        _navigationService = navigationService;

        BackCommand = ReactiveCommand.CreateFromTask(() => _navigationService.NavigateToAdminHomeAsync());
        RefreshCommand = ReactiveCommand.CreateFromTask(
            RefreshAsync,
            this.WhenAnyValue(x => x.IsBusy, b => !b));
        CreateOperatorCommand = ReactiveCommand.CreateFromTask(
            CreateOperatorAsync,
            this.WhenAnyValue(x => x.IsBusy, b => !b));
        DeleteUserCommand = ReactiveCommand.CreateFromTask(
            DeleteSelectedUserAsync,
            this.WhenAnyValue(
                x => x.SelectedServerUser,
                u => u is ApiUser sel
                     && !string.Equals(sel.Role, "admin", StringComparison.OrdinalIgnoreCase)
                     && sel.Id != (_authService.CurrentUser?.Id ?? -1)));

        SaveEditCommand = ReactiveCommand.CreateFromTask(
            SaveEditAsync,
            this.WhenAnyValue(x => x.IsBusy, b => !b));
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;
        _isInitialized = true;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        IsBusy = true;
        FeedbackMessage = string.Empty;
        try
        {
            var list = await _usersService.GetUsersAsync();
            ServerUsers = new ObservableCollection<ApiUser>(list ?? new List<ApiUser>());
        }
        catch (Exception ex)
        {
            FeedbackMessage = $"Не удалось загрузить список: {ex.Message}";
            FeedbackBrush = Brushes.OrangeRed;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateOperatorAsync()
    {
        if (string.IsNullOrWhiteSpace(NewOperatorUsername) || string.IsNullOrWhiteSpace(NewOperatorPassword))
        {
            FeedbackMessage = "Введите логин и пароль нового оператора";
            FeedbackBrush = Brushes.OrangeRed;
            return;
        }

        IsBusy = true;
        FeedbackMessage = string.Empty;
        try
        {
            await _usersService.CreateOperatorAsync(NewOperatorUsername.Trim(), NewOperatorPassword);
            NewOperatorUsername = string.Empty;
            NewOperatorPassword = string.Empty;
            FeedbackMessage = "Пользователь успешно создан";
            FeedbackBrush = Brushes.LimeGreen;
            await _journalService.RecordAsync(
                eventCode: "custom",
                message: "Администратор создал учётную запись оператора",
                source: "admin",
                action: "user_create",
                level: "info",
                usernameSnapshot: _authService.CurrentUser?.Username);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            FeedbackMessage = $"Ошибка: {ex.Message}";
            FeedbackBrush = Brushes.OrangeRed;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveEditAsync()
    {
        if (SelectedServerUser == null) return;
        if (string.IsNullOrWhiteSpace(EditUsername))
        {
            FeedbackMessage = "Логин не может быть пустым";
            FeedbackBrush = Brushes.OrangeRed;
            return;
        }

        IsBusy = true;
        FeedbackMessage = string.Empty;
        try
        {
            var password = string.IsNullOrWhiteSpace(EditPassword) ? null : EditPassword;
            var updated = await _usersService.UpdateUserAsync(
                SelectedServerUser.Id,
                EditUsername.Trim(),
                password,
                EditRole,
                null);
            FeedbackMessage = $"Пользователь «{updated?.Username}» обновлён";
            FeedbackBrush = Brushes.LimeGreen;
            await _journalService.RecordAsync(
                eventCode: "custom",
                message: $"Администратор изменил учётную запись «{updated?.Username}»",
                source: "admin",
                action: "user_edit",
                level: "info",
                usernameSnapshot: _authService.CurrentUser?.Username);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            FeedbackMessage = $"Ошибка: {ex.Message}";
            FeedbackBrush = Brushes.OrangeRed;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteSelectedUserAsync()
    {
        if (SelectedServerUser == null)
            return;

        if (string.Equals(SelectedServerUser.Role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            FeedbackMessage = "Учётную запись администратора удалить нельзя";
            FeedbackBrush = Brushes.OrangeRed;
            return;
        }

        if (SelectedServerUser.Id == _authService.CurrentUser?.Id)
        {
            FeedbackMessage = "Нельзя удалить свою учётную запись";
            FeedbackBrush = Brushes.OrangeRed;
            return;
        }

        IsBusy = true;
        FeedbackMessage = string.Empty;
        try
        {
            var deletedName = SelectedServerUser.Username;
            var deletedId = SelectedServerUser.Id;
            await _usersService.DeleteUserAsync(deletedId);
            SelectedServerUser = null;
            FeedbackMessage = $"Пользователь «{deletedName}» удалён";
            FeedbackBrush = Brushes.LimeGreen;
            await _journalService.RecordAsync(
                eventCode: "custom",
                message: $"Администратор удалил учётную запись «{deletedName}»",
                source: "admin",
                action: "user_delete",
                level: "warning",
                usernameSnapshot: _authService.CurrentUser?.Username);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            FeedbackMessage = $"Ошибка удаления: {ex.Message}";
            FeedbackBrush = Brushes.OrangeRed;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
