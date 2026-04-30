using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Client.ViewModels;
using ReactiveUI.Avalonia;

namespace Client;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();
    }
}