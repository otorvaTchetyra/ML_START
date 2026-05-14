using Avalonia.Markup.Xaml;
using Client.ViewModels;
using Avalonia.ReactiveUI;

namespace Client;

public partial class AdminUsersView : ReactiveUserControl<AdminUsersViewModel>
{
    public AdminUsersView()
    {
        InitializeComponent();
    }
}
