using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PropertyManagement.Pages
{
    public partial class ServiceRequestsPage : Page
    {
        private PropertyManagementEntities _context;
        private List<ServiceRequestViewModel> _allRequests;

        public class ServiceRequestViewModel
        {
            public int request_id { get; set; }
            public string request_type { get; set; }
            public string description { get; set; }
            public string status { get; set; }
            public DateTime? created_date { get; set; }
            public DateTime? completed_date { get; set; }
            public string building_address { get; set; }
            public string apartment_number { get; set; }
            public string employee_name { get; set; }
            public string full_address { get; set; }
            public string created_date_formatted => created_date?.ToString("dd.MM.yyyy HH:mm") ?? "Не указана";
            public string completed_date_formatted => completed_date?.ToString("dd.MM.yyyy HH:mm") ?? "Не выполнена";
        }

        public ServiceRequestsPage()
        {
            InitializeComponent();
            _context = new PropertyManagementEntities();
            Loaded += ServiceRequestsPage_Loaded;
        }

        private void ServiceRequestsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadRequests();
        }

        private void LoadRequests()
        {
            try
            {
                // Сначала получаем все данные без сложных вычислений
                var query = from r in _context.ServiceRequests
                            join a in _context.Apartments on r.apartment_id equals a.apartment_id
                            join b in _context.Buildings on a.building_id equals b.building_id
                            join e in _context.Employees on r.employee_id equals e.employee_id into employeeJoin
                            from e in employeeJoin.DefaultIfEmpty()
                            orderby r.created_date descending
                            select new
                            {
                                r.request_id,
                                r.request_type,
                                r.description,
                                r.status,
                                r.created_date,
                                r.completed_date,
                                b.address,
                                a.apartment_number,
                                employee_name = e != null ? e.full_name : "Не назначен"
                            };

                // Выполняем запрос и преобразуем в список
                var data = query.ToList();

                // Теперь создаем ViewModel на клиентской стороне
                _allRequests = data.Select(r => new ServiceRequestViewModel
                {
                    request_id = r.request_id,
                    request_type = r.request_type,
                    description = r.description,
                    status = r.status,
                    created_date = r.created_date,
                    completed_date = r.completed_date,
                    building_address = r.address,
                    apartment_number = r.apartment_number.ToString(),
                    employee_name = r.employee_name,
                    full_address = $"{r.address}, кв. {r.apartment_number}"
                }).ToList();

                ApplyFilters();
                UpdateCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (_allRequests == null || RequestsGrid == null)
            {
                return;
            }

            var filteredRequests = _allRequests;

            // Фильтр по статусу
            var selectedStatus = GetSelectedComboBoxItemContent(StatusFilterComboBox);
            if (!string.IsNullOrEmpty(selectedStatus) && selectedStatus != "Все статусы")
            {
                filteredRequests = filteredRequests.Where(r => r.status == selectedStatus).ToList();
            }

            // Фильтр по месяцу
            var selectedMonth = GetSelectedComboBoxItemContent(MonthFilterComboBox);
            if (!string.IsNullOrEmpty(selectedMonth) && selectedMonth != "Все месяцы")
            {
                int monthNumber = GetMonthNumber(selectedMonth);
                if (monthNumber > 0)
                {
                    filteredRequests = filteredRequests
                        .Where(r => r.created_date.HasValue && r.created_date.Value.Month == monthNumber)
                        .ToList();
                }
            }

            // Фильтр по поиску
            var searchText = SearchTextBox?.Text?.ToLower();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filteredRequests = filteredRequests
                    .Where(r => (r.request_type?.ToLower().Contains(searchText) ?? false) ||
                                (r.description?.ToLower().Contains(searchText) ?? false) ||
                                (r.building_address?.ToLower().Contains(searchText) ?? false) ||
                                (r.employee_name?.ToLower().Contains(searchText) ?? false))
                    .ToList();
            }

            RequestsGrid.ItemsSource = filteredRequests;
        }

        private string GetSelectedComboBoxItemContent(ComboBox comboBox)
        {
            if (comboBox?.SelectedItem == null)
                return null;

            if (comboBox.SelectedItem is ComboBoxItem item)
                return item.Content?.ToString();

            return comboBox.SelectedItem.ToString();
        }

        private int GetMonthNumber(string monthName)
        {
            switch (monthName)
            {
                case "Январь": return 1;
                case "Февраль": return 2;
                case "Март": return 3;
                case "Апрель": return 4;
                case "Май": return 5;
                case "Июнь": return 6;
                case "Июль": return 7;
                case "Август": return 8;
                case "Сентябрь": return 9;
                case "Октябрь": return 10;
                case "Ноябрь": return 11;
                case "Декабрь": return 12;
                default: return 0;
            }
        }

        private void UpdateCount()
        {
            if (RequestsCountText == null) return;

            if (RequestsGrid?.ItemsSource == null)
            {
                RequestsCountText.Text = " (0)";
                return;
            }

            var items = RequestsGrid.ItemsSource as System.Collections.IList;
            var count = items?.Count ?? 0;
            RequestsCountText.Text = $" ({count})";
        }

        private void AddRequest_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ServiceRequestEditPage());
        }

        private void EditRequest_Click(object sender, RoutedEventArgs e)
        {
            if (RequestsGrid?.SelectedItem is ServiceRequestViewModel selectedRequest)
            {
                NavigationService.Navigate(new ServiceRequestEditPage(selectedRequest.request_id));
            }
            else
            {
                MessageBox.Show("Выберите заявку для редактирования", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteRequest_Click(object sender, RoutedEventArgs e)
        {
            if (RequestsGrid?.SelectedItem is ServiceRequestViewModel selectedRequest)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить заявку #{selectedRequest.request_id}?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var requestToDelete = _context.ServiceRequests.Find(selectedRequest.request_id);
                        if (requestToDelete != null)
                        {
                            _context.ServiceRequests.Remove(requestToDelete);
                            _context.SaveChanges();
                            LoadRequests();

                            MessageBox.Show("Заявка успешно удалена", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления заявки: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите заявку для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadRequests();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
            UpdateCount();
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
            UpdateCount();
        }

        private void MonthFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
            UpdateCount();
        }

        private void RequestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RequestDetailsPanel == null || SelectedRequestInfo == null) return;

            if (RequestsGrid?.SelectedItem is ServiceRequestViewModel selectedRequest)
            {
                RequestDetailsPanel.Visibility = Visibility.Visible;
                SelectedRequestInfo.Text = $"Заявка #{selectedRequest.request_id}: {selectedRequest.request_type}\n" +
                                          $"Адрес: {selectedRequest.full_address}\n" +
                                          $"Создана: {selectedRequest.created_date_formatted}\n" +
                                          $"Статус: {selectedRequest.status}\n" +
                                          $"Исполнитель: {selectedRequest.employee_name}";
            }
            else
            {
                RequestDetailsPanel.Visibility = Visibility.Collapsed;
            }
        }
    }
}