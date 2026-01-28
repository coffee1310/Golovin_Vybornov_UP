using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PropertyManagement.Pages
{
    /// <summary>
    /// Логика взаимодействия для LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        private PropertyManagementEntities db = new PropertyManagementEntities();
        private bool isAuthenticating = false;

        public LoginPage()
        {
            InitializeComponent();
            txtLogin.Focus();

            // Для тестирования
            txtLogin.Text = "admin";
            txtPassword.Password = "admin";
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(login))
            {
                ShowError("Введите логин");
                return;
            }

            // Для тестирования создаем пользователя по логину
            CreateTestUser(login);
        }

        private void CreateTestUser(string login)
        {
            string positionName;

            // Определяем должность по логину
            if (login.Contains("admin"))
                positionName = "Администратор";
            else if (login.Contains("manager"))
                positionName = "Руководитель";
            else if (login.Contains("accountant"))
                positionName = "Бухгалтер";
            else if (login.Contains("technician"))
                positionName = "Техник";
            else if (login.Contains("dispatcher"))
                positionName = "Диспетчер";
            else
                positionName = "Пользователь";

            // Сохраняем данные
            Application.Current.Properties["EmployeeId"] = 1;
            Application.Current.Properties["FullName"] = "Тестовый пользователь (" + positionName + ")";
            Application.Current.Properties["Position"] = positionName;
            Application.Current.Properties["PositionId"] = 1;
            Application.Current.Properties["Login"] = login;
            Application.Current.Properties["PositionName"] = positionName;

            // Открываем главное окно
            var mainWindow = new MainWindow();
            mainWindow.Show();

            // Закрываем окно авторизации
            Window.GetWindow(this)?.Close();
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }
    }
}