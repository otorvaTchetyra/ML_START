using Client.Models;
using System;

namespace Client.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public string Greeting { get; set; }

        public MainWindowViewModel()
        {
            try
            {
                using var db = new AppDbContext();
                var canConnect = db.Database.CanConnect();
                Greeting = canConnect ? "БД подключена!" : "Ошибка подключения";
            }
            catch (Exception ex)
            {
                Greeting = $"Ошибка: {ex.Message}";
            }
        }
    }
}