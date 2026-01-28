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

            // Создаем и показываем окно авторизации
            var loginWindow = new Window
            {
                Title = "Авторизация",
                Content = new LoginPage(),
                WindowStyle = WindowStyle.None,
                WindowState = WindowState.Normal,
                ResizeMode = ResizeMode.NoResize,
                Width = 400,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            // Устанавливаем прозрачный фон для окна
            loginWindow.Background = System.Windows.Media.Brushes.Transparent;
            loginWindow.AllowsTransparency = true;

            loginWindow.Show();
        }
    }
}
