using PropertyManagement.Pages;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PropertyManagement
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Сначала показываем окно авторизации
            var loginWindow = new Window
            {
                Title = "Авторизация - Property Management",
                Content = new LoginPage(),
                WindowStyle = WindowStyle.SingleBorderWindow,
                WindowState = WindowState.Normal,
                ResizeMode = ResizeMode.NoResize,
                Width = 450,
                Height = 420,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = true
            };

            loginWindow.ShowDialog(); // Блокирующее диалоговое окно
        }
    }
}
