using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Client.Models;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;

namespace Client.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase, IScreen
    {
        public RoutingState Router { get; } = new RoutingState();
        public MainWindowViewModel(){ }
    }
}
