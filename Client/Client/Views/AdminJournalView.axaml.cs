using Avalonia.Markup.Xaml;
using Client.ViewModels;
using Avalonia.ReactiveUI;

namespace Client;

public partial class AdminJournalView : ReactiveUserControl<AdminJournalViewModel>
{
    public AdminJournalView()
    {
        InitializeComponent();
    }
}
