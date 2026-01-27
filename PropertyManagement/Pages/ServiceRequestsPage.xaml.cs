using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PropertyManagement.Pages
{
    public partial class ServiceRequestsPage : Page
    {
        private readonly PropertyManagementEntities _context;
        private List<ServiceRequestViewModel> _allRequests;
        private ServiceRequestViewModel _selectedRequest;

        public class ServiceRequestViewModel
        {
            public int request_id { get; set; }
            public string request_type { get; set; }
            public string description { get; set; }
            public string status { get; set; }
            public DateTime? created_date { get; set; }
            public int? apartment_id { get; set; }
            public int? employee_id { get; set; }
            public int? apartment_number { get; set; }
            public string building_address { get; set; }
            public string employee_name { get; set; }
        }

        public ServiceRequestsPage()
        {
            InitializeComponent();
            _context = new PropertyManagementEntities();
            LoadData();
            StatusFilterComboBox.SelectedIndex = 0;
        }

        private void LoadData()
        {
            try
            {
                // Создаем ViewModel с нужными данными
                _allRequests = (from request in _context.ServiceRequests
                                join apartment in _context.Apartments on request.apartment_id equals apartment.apartment_id into aptGroup
                                from apt in aptGroup.DefaultIfEmpty()
                                join building in _context.Buildings on apt.building_id equals building.building_id into bldGroup
                                from bld in bldGroup.DefaultIfEmpty()
                                join employee in _context.Employees on request.employee_id equals employee.employee_id into empGroup
                                from emp in empGroup.DefaultIfEmpty()
                                select new ServiceRequestViewModel
                                {
                                    request_id = request.request_id,
                                    request_type = request.request_type,
                                    description = request.description,
                                    status = request.status,
                                    created_date = request.created_date,
                                    apartment_id = request.apartment_id,
                                    employee_id = request.employee_id,
                                    apartment_number = apt != null ? apt.apartment_number : 0,
                                    building_address = bld != null ? bld.address : "Неизвестно",
                                    employee_name = emp != null ? emp.full_name : "Не назначен"
                                }).ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            var filtered = _allRequests.AsEnumerable();

            // Фильтр по статусу
            if (StatusFilterComboBox.SelectedItem != null &&
                StatusFilterComboBox.SelectedItem is ComboBoxItem selectedItem &&
                selectedItem.Content.ToString() != "Все")
            {
                string selectedStatus = selectedItem.Content.ToString();
                filtered = filtered.Where(r => r.status == selectedStatus);
            }

            // Поиск
            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                string search = SearchTextBox.Text.ToLower();
                filtered = filtered.Where(r =>
                    (r.request_type != null && r.request_type.ToLower().Contains(search)) ||
                    (r.description != null && r.description.ToLower().Contains(search)) ||
                    (r.building_address != null && r.building_address.ToLower().Contains(search)) ||
                    (r.employee_name != null && r.employee_name.ToLower().Contains(search)));
            }

            // Сортировка по дате (сначала новые)
            filtered = filtered.OrderByDescending(r => r.created_date);

            RequestsGrid.ItemsSource = filtered.ToList();
            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            try
            {
                int total = _allRequests.Count;
                int open = _allRequests.Count(r => r.status == "Открыта");
                int inProgress = _allRequests.Count(r => r.status == "В работе");
                int closed = _allRequests.Count(r => r.status == "Закрыта");

                RequestsCountText.Text = $" ({total} заявок: {open} открыто, {inProgress} в работе, {closed} закрыто)";
            }
            catch (Exception ex)
            {
                RequestsCountText.Text = $" ({_allRequests.Count} заявок)";
                Console.WriteLine($"Ошибка статистики: {ex.Message}");
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void AddRequest_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ServiceRequestEditPage());
        }

        private void EditRequest_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRequest == null)
            {
                MessageBox.Show("Выберите заявку для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NavigationService.Navigate(new ServiceRequestEditPage(_selectedRequest.request_id));
        }

        private void DeleteRequest_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRequest == null)
            {
                MessageBox.Show("Выберите заявку для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить заявку #{_selectedRequest.request_id}?\nТип: {_selectedRequest.request_type}",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var requestToDelete = _context.ServiceRequests.Find(_selectedRequest.request_id);
                    if (requestToDelete != null)
                    {
                        _context.ServiceRequests.Remove(requestToDelete);
                        _context.SaveChanges();

                        MessageBox.Show("Заявка удалена", "Успех",
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

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void RequestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedRequest = RequestsGrid.SelectedItem as ServiceRequestViewModel;
        }
    }
}