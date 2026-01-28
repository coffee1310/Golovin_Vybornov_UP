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
            ErrorText.Visibility = Visibility.Collapsed;
            ErrorText.Text = "";

            var errors = new List<string>();

            // Проверка заявки
            if (RequestComboBox.SelectedItem == null)
                errors.Add("• Выберите заявку");

            // Проверка типа расхода
            if (TypeComboBox.SelectedItem == null)
                errors.Add("• Выберите тип расхода");

            // Проверка суммы
            if (string.IsNullOrWhiteSpace(AmountTextBox.Text))
                errors.Add("• Введите сумму расхода");
            else if (!decimal.TryParse(AmountTextBox.Text, out decimal amount) || amount <= 0)
                errors.Add("• Сумма должна быть положительным числом");

            // Проверка даты
            if (!ExpenseDatePicker.SelectedDate.HasValue)
                errors.Add("• Выберите дату расхода");

            // Проверка кто добавил
            if (string.IsNullOrWhiteSpace(CreatedByTextBox.Text))
                errors.Add("• Укажите, кто добавил расход");

            if (errors.Any())
            {
                ErrorText.Text = string.Join("\n", errors);
                ErrorText.Visibility = Visibility.Visible;
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
    }
}
