using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Client.ViewModels;

namespace Client;

public partial class StreamView : ReactiveUserControl<StreamViewModel>
{
    public StreamView()
    {
        InitializeComponent();
    }
}