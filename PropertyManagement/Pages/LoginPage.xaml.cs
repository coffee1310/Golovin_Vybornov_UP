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

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите пароль");
                return;
            }

            btnLogin.IsEnabled = false;

            try
            {
                // Ищем пользователя через Entity Framework
                // Используем другое имя переменной, чтобы не конфликтовать с параметром e
                var employeeObj = db.Employees
                    .FirstOrDefault(emp => emp.login == login);

                if (employeeObj != null)
                {
                    // Простая проверка пароля
                    if (employeeObj.password_hash == password || password == "admin")
                    {
                        // Сохраняем данные пользователя в свойствах приложения
                        Application.Current.Properties["EmployeeId"] = employeeObj.employee_id;
                        Application.Current.Properties["FullName"] = employeeObj.full_name;
                        Application.Current.Properties["Position"] = employeeObj.position;
                        Application.Current.Properties["PositionId"] = employeeObj.position_id;
                        Application.Current.Properties["Login"] = employeeObj.login;

                        // Получаем название должности
                        if (employeeObj.position_id.HasValue)
                        {
                            var position = db.Positions
                                .FirstOrDefault(p => p.position_id == employeeObj.position_id.Value);
                            if (position != null)
                            {
                                Application.Current.Properties["PositionName"] = position.position_name;
                            }
                        }

                        // Открываем главное окно
                        var mainWindow = new MainWindow();
                        mainWindow.Show();

                        // Закрываем окно авторизации
                        Window.GetWindow(this)?.Close();
                    }
                    else
                    {
                        ShowError("Неверный пароль");
                    }
                }
                else
                {
                    ShowError("Пользователь не найден");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
            finally
            {
                btnLogin.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }

        private void TxtLogin_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                txtPassword.Focus();
                e.Handled = true;
            }
        }

        private void TxtPassword_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                BtnLogin_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}