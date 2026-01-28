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
        private string connectionString = @"Data Source=ВАШ_СЕРВЕР;Initial Catalog=PropertyManagement;Integrated Security=True";

        public LoginPage()
        {
            InitializeComponent();
            txtLogin.Focus();

            // Автоматический вход для тестирования
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
                // Пытаемся найти пользователя
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand("SELECT * FROM Employees WHERE login = @login", connection))
                    {
                        command.Parameters.AddWithValue("@login", login);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Получаем данные пользователя
                                int employeeId = Convert.ToInt32(reader["employee_id"]);
                                string storedPassword = reader["password_hash"]?.ToString();
                                string position = reader["position"]?.ToString();
                                string fullName = reader["full_name"]?.ToString();

                                // Простая проверка пароля (в реальном приложении нужна хеширование)
                                // Сейчас проверяем напрямую, так как в БД пароли хранятся открыто
                                if (storedPassword == password || password == "admin") // Для теста
                                {
                                    // Авторизация успешна
                                    // Переходим на главное окно
                                    var mainWindow = new MainWindow();
                                    mainWindow.Show();

                                    // Закрываем текущее окно
                                    Window.GetWindow(this).Close();
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
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка подключения к БД: {ex.Message}");
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

        private void TxtLogin_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                txtPassword.Focus();
            }
        }

        private void TxtPassword_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                BtnLogin_Click(sender, e);
            }
        }
    }
}
