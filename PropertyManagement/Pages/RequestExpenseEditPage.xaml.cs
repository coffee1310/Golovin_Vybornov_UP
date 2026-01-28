using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PropertyManagement.Pages
{
    public partial class RequestExpenseEditPage : Page
    {
        private readonly PropertyManagementEntities _context;
        private readonly int? _expenseId;
        private RequestExpenses _originalExpense;

        public class RequestViewModel
        {
            public int request_id { get; set; }
            public string DisplayText { get; set; }
        }

        private List<RequestViewModel> _requests;

        public RequestExpenseEditPage(int? expenseId = null)
        {
            InitializeComponent();
            _context = new PropertyManagementEntities();
            _expenseId = expenseId;

            // Устанавливаем значения по умолчанию
            ExpenseDatePicker.SelectedDate = DateTime.Today;
            CreatedByTextBox.Text = "Администратор";

            LoadFormData();
            LoadExpenseData();
        }

        private void LoadFormData()
        {
            try
            {
                // Загружаем список заявок
                _requests = (from r in _context.ServiceRequests
                             join a in _context.Apartments on r.apartment_id equals a.apartment_id
                             join b in _context.Buildings on a.building_id equals b.building_id
                             orderby r.request_id descending
                             select new
                             {
                                 request_id = r.request_id,
                                 address = b.address,
                                 apartment_number = a.apartment_number
                             }).ToList()
                            .Select(r => new RequestViewModel
                            {
                                request_id = r.request_id,
                                DisplayText = $"Заявка #{r.request_id} - {r.address}, кв. {r.apartment_number}"
                            }).ToList();

                RequestComboBox.ItemsSource = _requests;
                if (_requests.Any())
                    RequestComboBox.SelectedIndex = 0;

                // Устанавливаем значения для ComboBox типа расхода
                TypeComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных формы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadExpenseData()
        {
            try
            {
                if (_expenseId.HasValue)
                {
                    // Режим редактирования
                    _originalExpense = _context.RequestExpenses.Find(_expenseId.Value);

                    if (_originalExpense != null)
                    {
                        PageTitle.Text = "Редактирование расхода";

                        // Заполняем поля

                        // Заявка
                        var selectedRequest = _requests.FirstOrDefault(r =>
                            r.request_id == _originalExpense.request_id);
                        if (selectedRequest != null)
                            RequestComboBox.SelectedItem = selectedRequest;

                        // Тип расхода
                        if (!string.IsNullOrEmpty(_originalExpense.expense_type))
                        {
                            foreach (ComboBoxItem item in TypeComboBox.Items)
                            {
                                if (item.Content.ToString() == _originalExpense.expense_type)
                                {
                                    TypeComboBox.SelectedItem = item;
                                    break;
                                }
                            }
                        }

                        // Сумма
                        AmountTextBox.Text = _originalExpense.amount.ToString("F2");

                        // Описание
                        DescriptionTextBox.Text = _originalExpense.description ?? "";

                        // Дата
                        if (_originalExpense.expense_date.HasValue)
                            ExpenseDatePicker.SelectedDate = _originalExpense.expense_date.Value;

                        // Кто добавил
                        CreatedByTextBox.Text = _originalExpense.created_by ?? "Администратор";
                    }
                    else
                    {
                        MessageBox.Show("Расход не найден", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                    }
                }
                else
                {
                    // Режим добавления
                    PageTitle.Text = "Новый расход";
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
                if (_expenseId.HasValue)
                {
                    // Обновляем существующий расход
                    UpdateExistingExpense();
                }
                else
                {
                    // Создаем новый расход
                    CreateNewExpense();
                }

                _context.SaveChanges();

                MessageBox.Show(
                    _expenseId.HasValue ? "Расход обновлен!" : "Расход добавлен!",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Возвращаемся к списку расходов
                NavigationService.Navigate(new RequestExpensesPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateExistingExpense()
        {
            var selectedRequest = RequestComboBox.SelectedItem as RequestViewModel;
            if (selectedRequest != null)
            {
                _originalExpense.request_id = selectedRequest.request_id;
            }

            _originalExpense.expense_type = (TypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (decimal.TryParse(AmountTextBox.Text, out decimal amount))
            {
                _originalExpense.amount = amount;
            }

            _originalExpense.description = DescriptionTextBox.Text.Trim();
            _originalExpense.expense_date = ExpenseDatePicker.SelectedDate;
            _originalExpense.created_by = CreatedByTextBox.Text.Trim();
        }

        private void CreateNewExpense()
        {
            var selectedRequest = RequestComboBox.SelectedItem as RequestViewModel;
            if (selectedRequest == null)
            {
                throw new Exception("Не выбрана заявка");
            }

            if (!decimal.TryParse(AmountTextBox.Text, out decimal amount))
            {
                throw new Exception("Неверная сумма расхода");
            }

            var newExpense = new RequestExpenses
            {
                request_id = selectedRequest.request_id,
                expense_type = (TypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(),
                description = DescriptionTextBox.Text.Trim(),
                amount = amount,
                expense_date = ExpenseDatePicker.SelectedDate,
                created_by = CreatedByTextBox.Text.Trim()
            };

            _context.RequestExpenses.Add(newExpense);
        }

        private bool ValidateData()
        {
            var errors = new List<string>();

            // Проверка заявки (обязательное)
            if (RequestComboBox.SelectedItem == null)
                errors.Add("• Выберите заявку");

            // Проверка типа расхода (обязательное)
            if (TypeComboBox.SelectedItem == null)
                errors.Add("• Выберите тип расхода");

            // Проверка суммы (обязательное)
            if (string.IsNullOrWhiteSpace(AmountTextBox.Text))
                errors.Add("• Введите сумму расхода");
            else if (!decimal.TryParse(AmountTextBox.Text, out decimal amount))
                errors.Add("• Сумма должна быть числом");
            else if (amount <= 0)
                errors.Add("• Сумма должна быть больше 0");
            else if (amount > 999999.99m)
                errors.Add("• Сумма слишком велика (максимум 999 999.99)");

            // Проверка даты (обязательное)
            if (!ExpenseDatePicker.SelectedDate.HasValue)
                errors.Add("• Выберите дату расхода");
            else if (ExpenseDatePicker.SelectedDate.Value > DateTime.Today)
                errors.Add("• Дата расхода не может быть в будущем");
            else if (ExpenseDatePicker.SelectedDate.Value < DateTime.Today.AddYears(-10))
                errors.Add("• Дата расхода слишком старая");

            // Проверка кто добавил (обязательное)
            if (string.IsNullOrWhiteSpace(CreatedByTextBox.Text))
                errors.Add("• Укажите, кто добавил расход");
            else if (CreatedByTextBox.Text.Trim().Length < 2)
                errors.Add("• Имя должно содержать минимум 2 символа");
            else if (CreatedByTextBox.Text.Trim().Length > 100)
                errors.Add("• Имя не может превышать 100 символов");

            // Проверка описания (необязательное, но проверяем длину если заполнено)
            if (!string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                if (DescriptionTextBox.Text.Trim().Length > 1000)
                    errors.Add("• Описание не может превышать 1000 символов");
            }

            if (errors.Count > 0)
            {
                var errorMessage = "Пожалуйста, исправьте следующие ошибки:\n\n" +
                                  string.Join("\n", errors);

                MessageBox.Show(errorMessage, "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                return false;
            }

            return true;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RequestExpensesPage());
        }

        // Обработчик для проверки ввода суммы (только цифры и точка)
        private void AmountTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            string newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            // Разрешаем только цифры и точку
            if (!char.IsDigit(e.Text[0]) && e.Text[0] != '.')
            {
                e.Handled = true;
                return;
            }

            // Проверяем, что точка только одна
            if (e.Text[0] == '.' && (textBox.Text.Contains('.') || textBox.SelectionStart == 0))
            {
                e.Handled = true;
                return;
            }

            // Проверяем, что после точки не более 2 цифр
            if (textBox.Text.Contains('.'))
            {
                int decimalIndex = textBox.Text.IndexOf('.');
                int selectionStart = textBox.SelectionStart;

                // Если вводим после точки и уже есть 2 цифры
                if (selectionStart > decimalIndex)
                {
                    string afterDecimal = textBox.Text.Substring(decimalIndex + 1);
                    if (afterDecimal.Length >= 2 && selectionStart > decimalIndex + 1)
                    {
                        e.Handled = true;
                        return;
                    }
                }
            }

            // Проверяем общую длину
            if (newText.Length > 10) // Максимум 10 символов
            {
                e.Handled = true;
            }
        }

        // Обработчик для удаления лишних нулей при потере фокуса
        private void AmountTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(AmountTextBox.Text, out decimal amount))
            {
                AmountTextBox.Text = amount.ToString("F2");
            }
        }

        // Обработчик для проверки длины описания
        private void DescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DescriptionTextBox.Text.Length > 1000)
            {
                DescriptionTextBox.Text = DescriptionTextBox.Text.Substring(0, 1000);
                DescriptionTextBox.CaretIndex = 1000;

                MessageBox.Show("Длина описания не может превышать 1000 символов",
                    "Предупреждение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        // Обработчик для проверки длины имени
        private void CreatedByTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CreatedByTextBox.Text.Length > 100)
            {
                CreatedByTextBox.Text = CreatedByTextBox.Text.Substring(0, 100);
                CreatedByTextBox.CaretIndex = 100;
            }
        }
    }
}