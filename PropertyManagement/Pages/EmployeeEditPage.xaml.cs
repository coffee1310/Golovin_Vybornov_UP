using System;
using System.Linq; // Добавлено для использования .Any()
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace PropertyManagement.Pages
{
    public partial class EmployeeEditPage : Page
    {
        private readonly PropertyManagementEntities _context;
        private readonly int? _employeeId;
        private Employees _originalEmployee;

        public EmployeeEditPage(int? employeeId = null)
        {
            InitializeComponent();
            _context = new PropertyManagementEntities();
            _employeeId = employeeId;

            // Подписываемся на события ввода
            PhoneTextBox.TextChanged += PhoneTextBox_TextChanged;

            LoadEmployeeData();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void LoadEmployeeData()
        {
            try
            {
                if (_employeeId.HasValue)
                {
                    // Режим редактирования
                    _originalEmployee = _context.Employees.Find(_employeeId.Value);

                    if (_originalEmployee != null)
                    {
                        PageTitle.Text = "Редактирование сотрудника";

                        // Заполняем поля
                        FullNameTextBox.Text = _originalEmployee.full_name ?? "";
                        PhoneTextBox.Text = _originalEmployee.phone_number ?? "";
                        LoginTextBox.Text = _originalEmployee.login ?? "";

                        // Устанавливаем должность в ComboBox
                        if (!string.IsNullOrEmpty(_originalEmployee.position))
                        {
                            foreach (ComboBoxItem item in PositionComboBox.Items)
                            {
                                if (item.Content.ToString() == _originalEmployee.position)
                                {
                                    PositionComboBox.SelectedItem = item;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Сотрудник не найден", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                    }
                }
                else
                {
                    // Режим добавления
                    PageTitle.Text = "Добавление нового сотрудника";
                    PositionComboBox.SelectedIndex = 0; // Выбираем первую должность по умолчанию
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                NavigationService.GoBack();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateData())
            {
                return;
            }

            try
            {
                if (_employeeId.HasValue)
                {
                    // Обновляем существующего сотрудника
                    UpdateExistingEmployee();
                }
                else
                {
                    // Создаем нового сотрудника
                    CreateNewEmployee();
                }

                _context.SaveChanges();

                MessageBox.Show(
                    _employeeId.HasValue ? "Сотрудник успешно обновлен!" : "Сотрудник успешно добавлен!",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Возвращаемся к списку сотрудников
                NavigationService.Navigate(new EmployeesPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateExistingEmployee()
        {
            try
            {
                _originalEmployee.full_name = FullNameTextBox.Text.Trim();
                _originalEmployee.phone_number = PhoneTextBox.Text.Trim();
                _originalEmployee.login = LoginTextBox.Text.Trim();

                // Получаем выбранную должность из ComboBox
                if (PositionComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    _originalEmployee.position = selectedItem.Content.ToString();
                }
                else
                {
                    throw new Exception("Должность не выбрана");
                }

                // Если вводится новый пароль
                if (!string.IsNullOrWhiteSpace(PasswordBox.Password) &&
                    !string.IsNullOrWhiteSpace(ConfirmPasswordBox.Password))
                {
                    if (PasswordBox.Password == ConfirmPasswordBox.Password)
                    {
                        // Здесь должна быть хеширование пароля
                        _originalEmployee.password_hash = PasswordBox.Password; // В реальности нужно хешировать
                    }
                    else
                    {
                        throw new Exception("Пароли не совпадают");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка обновления данных: {ex.Message}");
            }
        }

        private void CreateNewEmployee()
        {
            try
            {
                // Получаем выбранную должность из ComboBox
                string position = "";
                if (PositionComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    position = selectedItem.Content.ToString();
                }
                else
                {
                    throw new Exception("Должность не выбрана");
                }

                var newEmployee = new Employees
                {
                    full_name = FullNameTextBox.Text.Trim(),
                    position = position,
                    phone_number = PhoneTextBox.Text.Trim(),
                    login = LoginTextBox.Text.Trim()
                };

                // Проверка пароля для нового сотрудника
                if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    throw new Exception("Пароль обязателен для нового сотрудника");
                }

                if (PasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    throw new Exception("Пароли не совпадают");
                }

                // Здесь должна быть хеширование пароля
                newEmployee.password_hash = PasswordBox.Password; // В реальности нужно хешировать

                _context.Employees.Add(newEmployee);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка создания сотрудника: {ex.Message}");
            }
        }

        private bool ValidateData()
        {
            var errors = new System.Collections.Generic.List<string>();

            // Проверка ФИО (обязательное)
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
                errors.Add("• Введите ФИО сотрудника");
            else if (FullNameTextBox.Text.Trim().Length < 5)
                errors.Add("• ФИО должно содержать минимум 5 символов");
            else if (FullNameTextBox.Text.Trim().Length > 255)
                errors.Add("• ФИО не может превышать 255 символов");

            // Проверка должности (обязательное)
            if (PositionComboBox.SelectedItem == null)
                errors.Add("• Выберите должность сотрудника");

            // Проверка телефона (обязательное)
            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
                errors.Add("• Введите номер телефона");
            else
            {
                // Убираем все нецифровые символы
                string phoneDigits = Regex.Replace(PhoneTextBox.Text, @"\D", "");

                if (phoneDigits.Length != 11 && phoneDigits.Length != 10)
                    errors.Add("• Телефон должен содержать 10 или 11 цифр");
                else if (!IsValidPhoneNumber(PhoneTextBox.Text))
                    errors.Add("• Введите корректный номер телефона (например: +7(XXX)XXX-XX-XX или 8XXXXXXXXXX)");

                // Проверка уникальности телефона
                var existingEmployeeByPhone = _context.Employees
                    .FirstOrDefault(o => o.phone_number == PhoneTextBox.Text.Trim() &&
                                       (!_employeeId.HasValue || o.employee_id != _employeeId.Value));

                if (existingEmployeeByPhone != null)
                    errors.Add($"• Сотрудник с телефоном '{PhoneTextBox.Text.Trim()}' уже существует в системе");
            }

            // Проверка логина (обязательное)
            if (string.IsNullOrWhiteSpace(LoginTextBox.Text))
                errors.Add("• Введите логин");
            else if (LoginTextBox.Text.Trim().Length < 3)
                errors.Add("• Логин должен содержать минимум 3 символа");
            else if (LoginTextBox.Text.Trim().Length > 50)
                errors.Add("• Логин не может превышать 50 символов");
            else if (!IsValidLogin(LoginTextBox.Text))
                errors.Add("• Логин может содержать только латинские буквы, цифры и символы подчеркивания");
            else
            {
                // Проверка уникальности логина
                var existingEmployeeByLogin = _context.Employees
                    .FirstOrDefault(o => o.login == LoginTextBox.Text.Trim() &&
                                       (!_employeeId.HasValue || o.employee_id != _employeeId.Value));

                if (existingEmployeeByLogin != null)
                    errors.Add($"• Сотрудник с логином '{LoginTextBox.Text.Trim()}' уже существует в системе");
            }

            // Проверка пароля
            if (!_employeeId.HasValue)
            {
                // Для нового сотрудника пароль обязателен
                if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                    errors.Add("• Введите пароль");
                else if (PasswordBox.Password.Length < 6)
                    errors.Add("• Пароль должен содержать минимум 6 символов");
            }
            else
            {
                // Для редактирования: если меняется пароль, то нужны оба поля
                if (!string.IsNullOrWhiteSpace(PasswordBox.Password) ||
                    !string.IsNullOrWhiteSpace(ConfirmPasswordBox.Password))
                {
                    if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                        errors.Add("• Введите новый пароль");
                    else if (string.IsNullOrWhiteSpace(ConfirmPasswordBox.Password))
                        errors.Add("• Подтвердите новый пароль");
                }
            }

            // Проверка подтверждения пароля
            if (!string.IsNullOrWhiteSpace(PasswordBox.Password) &&
                !string.IsNullOrWhiteSpace(ConfirmPasswordBox.Password))
            {
                if (PasswordBox.Password != ConfirmPasswordBox.Password)
                    errors.Add("• Пароли не совпадают");
            }

            // Отображение ошибок
            if (errors.Count > 0) // Исправлено с .Any() на .Count > 0
            {
                var errorMessage = "Пожалуйста, исправьте следующие ошибки:\n\n" +
                                  string.Join("\n", errors);

                MessageBox.Show(errorMessage, "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                return false;
            }

            return true;
        }

        private bool IsValidPhoneNumber(string phone)
        {
            // Проверяем несколько форматов телефонов
            var patterns = new[]
            {
                @"^\+7\(\d{3}\)\d{3}-\d{2}-\d{2}$", // +7(XXX)XXX-XX-XX
                @"^8\d{10}$", // 8XXXXXXXXXX
                @"^\+7\d{10}$", // +7XXXXXXXXXX
                @"^\d{10}$", // XXXXXXXXXX
                @"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$" // +7 (XXX) XXX-XX-XX
            };

            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(phone, pattern))
                    return true;
            }

            return false;
        }

        private bool IsValidLogin(string login)
        {
            // Логин может содержать только латинские буквы, цифры и подчеркивание
            return Regex.IsMatch(login, @"^[a-zA-Z0-9_]+$");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new EmployeesPage());
        }

        // Маска для телефона
        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Автоматическое форматирование телефона
            if (PhoneTextBox.IsFocused)
            {
                string text = PhoneTextBox.Text;
                string digitsOnly = Regex.Replace(text, @"\D", "");

                if (digitsOnly.Length <= 1)
                {
                    if (!text.StartsWith("+") && !text.StartsWith("8"))
                    {
                        PhoneTextBox.Text = "8";
                        PhoneTextBox.CaretIndex = 1;
                    }
                }
                else if (digitsOnly.Length == 11)
                {
                    if (digitsOnly.StartsWith("7") || digitsOnly.StartsWith("8"))
                    {
                        string formatted = $"+7 ({digitsOnly.Substring(1, 3)}) {digitsOnly.Substring(4, 3)}-{digitsOnly.Substring(7, 2)}-{digitsOnly.Substring(9, 2)}";
                        PhoneTextBox.Text = formatted;
                    }
                }
            }
        }
    }
}