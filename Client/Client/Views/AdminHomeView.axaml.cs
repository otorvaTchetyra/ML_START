using Avalonia.Markup.Xaml;
using Client.ViewModels;
using Avalonia.ReactiveUI;

namespace Client;

public partial class AdminHomeView : ReactiveUserControl<AdminHomeViewModel>
{
    public AdminHomeView()
    {
        InitializeComponent();
    }
}
