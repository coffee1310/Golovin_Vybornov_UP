using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PropertyManagement.Pages
{
    public partial class RequestExpensesPage : Page
    {
        private readonly PropertyManagementEntities _context;
        private List<ExpenseViewModel> _allExpenses;
        private ExpenseViewModel _selectedExpense;

        public class ExpenseViewModel
        {
            public int expense_id { get; set; }
            public int request_id { get; set; }
            public string RequestInfo { get; set; }
            public string expense_type { get; set; }
            public string description { get; set; }
            public decimal amount { get; set; }
            public DateTime? expense_date { get; set; }
            public string created_by { get; set; }
        }

        public RequestExpensesPage()
        {
            InitializeComponent();
            _context = new PropertyManagementEntities();
            LoadData();
            TypeFilterComboBox.SelectedIndex = 0;
        }

        private void LoadData()
        {
            try
            {
                // Загружаем расходы с информацией о заявках
                _allExpenses = (from e in _context.RequestExpenses
                                join r in _context.ServiceRequests on e.request_id equals r.request_id
                                join a in _context.Apartments on r.apartment_id equals a.apartment_id
                                join b in _context.Buildings on a.building_id equals b.building_id
                                orderby e.expense_date descending
                                select new
                                {
                                    e.expense_id,
                                    e.request_id,
                                    address = b.address,
                                    apartment_number = a.apartment_number,
                                    e.expense_type,
                                    e.description,
                                    e.amount,
                                    e.expense_date,
                                    e.created_by
                                }).ToList()
                               .Select(e => new ExpenseViewModel
                               {
                                   expense_id = e.expense_id,
                                   request_id = e.request_id,
                                   RequestInfo = $"Заявка #{e.request_id}",
                                   expense_type = e.expense_type,
                                   description = e.description,
                                   amount = e.amount,
                                   expense_date = e.expense_date,
                                   created_by = e.created_by ?? "Система"
                               }).ToList();

                ApplyFilters();

                // Обновляем статистику
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных о расходах: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            var filtered = _allExpenses.AsEnumerable();

            // Фильтр по типу расхода
            if (TypeFilterComboBox.SelectedItem != null &&
                TypeFilterComboBox.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.Content.ToString() != "Все типы")
            {
                string selectedType = selectedItem.Content.ToString();
                filtered = filtered.Where(e => e.expense_type == selectedType);
            }

            // Поиск
            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                string search = SearchTextBox.Text.ToLower();
                filtered = filtered.Where(e =>
                    (e.description != null && e.description.ToLower().Contains(search)) ||
                    e.request_id.ToString().Contains(search));
            }

            ExpensesGrid.ItemsSource = filtered.ToList();
            ExpensesCountText.Text = $" ({filtered.Count()} расходов)";
        }

        private void UpdateStatistics()
        {
            try
            {
                // Общая сумма расходов
                decimal totalExpenses = _allExpenses.Sum(e => e.amount);
                // Средний расход на заявку
                decimal avgExpensePerRequest = _allExpenses.Any() ?
                    totalExpenses / _allExpenses.Select(e => e.request_id).Distinct().Count() : 0;

                // Можно добавить отображение статистики на странице
                Console.WriteLine($"Общие расходы: {totalExpenses:N2} ₽");
                Console.WriteLine($"Средний расход на заявку: {avgExpensePerRequest:N2} ₽");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления статистики: {ex.Message}");
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void AddExpense_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RequestExpenseEditPage());
        }

        private void EditExpense_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedExpense == null)
            {
                MessageBox.Show("Выберите расход для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NavigationService.Navigate(new RequestExpenseEditPage(_selectedExpense.expense_id));
        }

        private void DeleteExpense_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedExpense == null)
            {
                MessageBox.Show("Выберите расход для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить расход на сумму {_selectedExpense.amount:N2} ₽?\n" +
                $"Тип: {_selectedExpense.expense_type}\n" +
                $"Описание: {_selectedExpense.description}",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var expenseToDelete = _context.RequestExpenses.Find(_selectedExpense.expense_id);

                    if (expenseToDelete != null)
                    {
                        _context.RequestExpenses.Remove(expenseToDelete);
                        _context.SaveChanges();

                        MessageBox.Show("Расход удален", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        LoadData();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void TypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ExpensesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedExpense = ExpensesGrid.SelectedItem as ExpenseViewModel;
        }
    }
}