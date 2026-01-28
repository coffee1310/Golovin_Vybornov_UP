using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PropertyManagement.Pages
{
    public partial class LoginPage : Page
    {
        private bool isAuthenticating = false;

        public LoginPage()
        {
            InitializeComponent();

            // Фокус на поле логина
            txtLogin.Focus();

            // Установка тестовых данных (удалить в продакшене)
            txtLogin.Text = "admin";
            txtPassword.Password = "admin";
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            AttemptLogin();
        }

        private void AttemptLogin()
        {
            // Если уже идет процесс аутентификации, не начинать новый
            if (isAuthenticating)
                return;

            // Сбрасываем подсветку полей
            ResetFieldBorders();

            // Валидация полей
            if (!ValidateInput())
                return;

            // Блокируем кнопку и поля
            SetAuthenticatingState(true);

            try
            {
                // Получаем данные из полей
                string login = txtLogin.Text.Trim();
                string password = txtPassword.Password;

                using (var db = new PropertyManagementEntities())
                {
                    // Ищем сотрудника с указанным логином и паролем
                    var employee = db.Employees
                        .FirstOrDefault(emp => emp.login == login &&
                                               emp.password_hash == password);

                    if (employee != null)
                    {
                        // Получаем название должности
                        string positionName = employee.position ?? "Не указана";

                        // Проверяем активность пользователя (если есть поле IsActive)
                        bool isActive = true;
                        var isActiveProperty = employee.GetType().GetProperty("IsActive");
                        if (isActiveProperty != null)
                        {
                            isActive = (bool)(isActiveProperty.GetValue(employee) ?? true);
                        }

                        if (!isActive)
                        {
                            MessageBox.Show("Учетная запись заблокирована. Обратитесь к администратору.",
                                "Ошибка входа",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }

                        // Сохраняем данные пользователя
                        Application.Current.Properties["EmployeeId"] = employee.employee_id;
                        Application.Current.Properties["FullName"] = employee.full_name;
                        Application.Current.Properties["Position"] = positionName;
                        Application.Current.Properties["Login"] = employee.login;

                        // Логируем вход (если есть таблица для логов)
                        LogLogin(employee.employee_id);

                        // Открываем главное окно
                        var mainWindow = new MainWindow();
                        mainWindow.Show();

                        // Закрываем окно входа
                        var loginWindow = Window.GetWindow(this);
                        loginWindow?.Close();
                    }
                    else
                    {
                        // Проверяем существование пользователя
                        bool userExists = db.Employees.Any(emp => emp.login == login);

                        string errorMessage = userExists
                            ? "Неверный пароль. Проверьте правильность ввода."
                            : "Пользователь с таким логином не найден.";

                        MessageBox.Show(errorMessage,
                            "Ошибка входа",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        // Подсвечиваем поле пароля при неверном пароле
                        if (userExists)
                        {
                            txtPassword.BorderBrush = Brushes.Red;
                            txtPassword.Focus();
                            txtPassword.SelectAll();
                        }
                        else
                        {
                            txtLogin.BorderBrush = Brushes.Red;
                            txtLogin.Focus();
                            txtLogin.SelectAll();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подключении к базе данных:\n{ex.Message}",
                    "Ошибка подключения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                // Разблокируем кнопку и поля
                SetAuthenticatingState(false);
            }
        }

        private bool ValidateInput()
        {
            var errors = new System.Collections.Generic.List<string>();

            // Проверка логина
            string login = txtLogin.Text.Trim();
            if (string.IsNullOrWhiteSpace(login))
            {
                errors.Add("• Введите логин");
                txtLogin.BorderBrush = Brushes.Red;
            }
            else if (login.Length < 3)
            {
                errors.Add("• Логин должен содержать минимум 3 символа");
                txtLogin.BorderBrush = Brushes.Red;
            }
            else if (login.Length > 50)
            {
                errors.Add("• Логин не может превышать 50 символов");
                txtLogin.BorderBrush = Brushes.Red;
            }

            // Проверка пароля
            string password = txtPassword.Password;
            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("• Введите пароль");
                txtPassword.BorderBrush = Brushes.Red;
            }
            else if (password.Length < 6)
            {
                errors.Add("• Пароль должен содержать минимум 6 символов");
                txtPassword.BorderBrush = Brushes.Red;
            }

            // Если есть ошибки, показываем их в MessageBox
            if (errors.Count > 0)
            {
                string errorMessage = "Пожалуйста, исправьте следующие ошибки:\n\n" +
                                     string.Join("\n", errors);

                MessageBox.Show(errorMessage,
                    "Ошибка ввода данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // Устанавливаем фокус на первое поле с ошибкой
                if (txtLogin.BorderBrush == Brushes.Red)
                {
                    txtLogin.Focus();
                    txtLogin.SelectAll();
                }
                else if (txtPassword.BorderBrush == Brushes.Red)
                {
                    txtPassword.Focus();
                    txtPassword.SelectAll();
                }

                return false;
            }

            return true;
        }

        private void ResetFieldBorders()
        {
            // Восстанавливаем стандартный цвет рамок
            txtLogin.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4C8B3"));
            txtPassword.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4C8B3"));
        }

        private void SetAuthenticatingState(bool isAuthenticating)
        {
            this.isAuthenticating = isAuthenticating;
            btnLogin.IsEnabled = !isAuthenticating;
            txtLogin.IsEnabled = !isAuthenticating;
            txtPassword.IsEnabled = !isAuthenticating;

            if (isAuthenticating)
            {
                btnLogin.Content = "Проверка...";
                Cursor = Cursors.Wait;
            }
            else
            {
                btnLogin.Content = "Войти";
                Cursor = Cursors.Arrow;
            }
        }

        private void LogLogin(int employeeId)
        {
            try
            {
                using (var db = new PropertyManagementEntities())
                {
                    // Проверяем существование таблицы LoginLogs
                    var tableExists = db.Database.SqlQuery<int?>(
                        "SELECT OBJECT_ID('LoginLogs', 'U')").FirstOrDefault();

                    if (tableExists != null)
                    {
                        // Создаем запись в логе
                        db.Database.ExecuteSqlCommand(
                            "INSERT INTO LoginLogs (employee_id, login_date, login_time) VALUES ({0}, {1}, {2})",
                            employeeId, DateTime.Today, DateTime.Now.TimeOfDay);
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки логирования
            }
        }

        // Обработчики нажатия Enter в полях ввода
        private void TxtLogin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txtPassword.Focus();
                e.Handled = true;
            }
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AttemptLogin();
                e.Handled = true;
            }
        }

        // Обработчики для сброса подсветки при изменении текста
        private void TxtLogin_TextChanged(object sender, TextChangedEventArgs e)
        {
            ResetFieldBorders();
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ResetFieldBorders();
        }
    }
}