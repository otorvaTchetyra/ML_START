using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Client.ViewModels;
using ReactiveUI.Avalonia;

namespace Client;

public partial class SettingsView : ReactiveUserControl<SettingsViewModel>
{
    public SettingsView()
    {
        InitializeComponent();
    }
}