using System;
using System.Linq;
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
                        PositionTextBox.Text = _originalEmployee.position ?? "";
                        PhoneTextBox.Text = _originalEmployee.phone_number ?? "";
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
                    _employeeId.HasValue ? "Сотрудник обновлен!" : "Сотрудник добавлен!",
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
            _originalEmployee.full_name = FullNameTextBox.Text.Trim();
            _originalEmployee.position = PositionTextBox.Text.Trim();
            _originalEmployee.phone_number = PhoneTextBox.Text.Trim();
        }

        private void CreateNewEmployee()
        {
            var newEmployee = new Employees
            {
                full_name = FullNameTextBox.Text.Trim(),
                position = PositionTextBox.Text.Trim(),
                phone_number = PhoneTextBox.Text.Trim(),
            };

            _context.Employees.Add(newEmployee);
        }

        private bool ValidateData()
        {
            ErrorText.Visibility = Visibility.Collapsed;
            ErrorText.Text = "";

            var errors = "";

            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
                errors += "• ФИО обязательно\n";

            // Проверка телефона
            if (!string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                // Убираем все нецифровые символы
                string phoneDigits = Regex.Replace(PhoneTextBox.Text, @"\D", "");

                if (phoneDigits.Length < 10 || phoneDigits.Length > 11)
                    errors += "• Телефон: введите 10-11 цифр\n";
            }

            // Проверка email
            if (!string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                try
                {
                    var addr = new System.Net.Mail.MailAddress(EmailTextBox.Text);
                    if (addr.Address != EmailTextBox.Text.Trim())
                        errors += "• Email: неверный формат\n";
                }
                catch
                {
                    errors += "• Email: неверный формат\n";
                }
            }

            if (!string.IsNullOrEmpty(errors))
            {
                ErrorText.Text = errors;
                ErrorText.Visibility = Visibility.Visible;
                return false;
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Возвращаемся к списку сотрудников
            NavigationService.Navigate(new EmployeesPage());
        }
    }
}