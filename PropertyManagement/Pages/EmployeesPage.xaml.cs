using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PropertyManagement.Pages
{
    public partial class EmployeesPage : Page
    {
        private readonly PropertyManagementEntities _context;
        private List<Employees> _allEmployees;
        private Employees _selectedEmployee;

        public EmployeesPage()
        {
            InitializeComponent();
            _context = new PropertyManagementEntities();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _allEmployees = _context.Employees.ToList();
                ApplyFilters();
                LoadPositionFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPositionFilter()
        {
            try
            {
                var positions = _allEmployees
                    .Select(e => e.position)
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                PositionFilterComboBox.Items.Clear();
                PositionFilterComboBox.Items.Add("Все должности");

                foreach (var position in positions)
                {
                    PositionFilterComboBox.Items.Add(position);
                }

                PositionFilterComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки фильтра: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            var filtered = _allEmployees.AsEnumerable();

            // Поиск
            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                string search = SearchTextBox.Text.ToLower();
                filtered = filtered.Where(e =>
                    (e.full_name != null && e.full_name.ToLower().Contains(search)) ||
                    (e.position != null && e.position.ToLower().Contains(search)) ||
                    (e.phone_number != null && e.phone_number.Contains(search)));
            }

            // Фильтр по должности
            if (PositionFilterComboBox.SelectedItem != null &&
                PositionFilterComboBox.SelectedItem.ToString() != "Все должности")
            {
                string selectedPosition = PositionFilterComboBox.SelectedItem.ToString();
                filtered = filtered.Where(e => e.position == selectedPosition);
            }

            EmployeesGrid.ItemsSource = filtered.ToList();
            EmployeesCountText.Text = $" ({filtered.Count()} сотрудников)";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new EmployeeEditPage());
        }

        private void EditEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee == null)
            {
                MessageBox.Show("Выберите сотрудника для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NavigationService.Navigate(new EmployeeEditPage(_selectedEmployee.employee_id));
        }

        private void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee == null)
            {
                MessageBox.Show("Выберите сотрудника для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить сотрудника: {_selectedEmployee.full_name}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Проверяем, есть ли связанные заявки
                    var hasRequests = _context.ServiceRequests.Any(r => r.employee_id == _selectedEmployee.employee_id);

                    if (hasRequests)
                    {
                        MessageBox.Show("Невозможно удалить сотрудника, так как у него есть заявки. " +
                            "Сначала переназначьте или удалите заявки этого сотрудника.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    _context.Employees.Remove(_selectedEmployee);
                    _context.SaveChanges();

                    MessageBox.Show("Сотрудник удален", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadData();
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

        private void PositionFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void EmployeesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedEmployee = EmployeesGrid.SelectedItem as Employees;
        }
    }
}