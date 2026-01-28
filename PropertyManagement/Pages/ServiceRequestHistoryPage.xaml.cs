using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Globalization;
using System.IO;
using Microsoft.Win32;
using System.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.ComponentModel;

namespace PropertyManagement.Pages
{
    public partial class ServiceRequestHistoryPage : Page
    {
        private string connectionString;
        private ObservableCollection<RequestDisplayItem> allRequests = new ObservableCollection<RequestDisplayItem>();
        private List<RequestType> requestTypes = new List<RequestType>();
        private List<Employee> employees = new List<Employee>();

        public ServiceRequestHistoryPage()
        {
            InitializeComponent();
            // Используем строку подключения напрямую для совместимости
            connectionString = "Data Source=.;Initial Catalog=PropertyManagement;Integrated Security=True";
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем, что все элементы инициализированы
                if (dpDateTo == null || dpDateFrom == null ||
                    cmbStatus == null || cmbRequestType == null ||
                    cmbEmployee == null || txtSearch == null ||
                    btnAll == null || btnToday == null ||
                    btnWeek == null || btnMonth == null ||
                    btnQuarter == null || btnYear == null)
                {
                    MessageBox.Show("Некоторые элементы управления не инициализированы", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Устанавливаем дату "по" на сегодня
                dpDateTo.SelectedDate = DateTime.Today;
                dpDateFrom.SelectedDate = DateTime.Today.AddMonths(-1);

                // Подписываемся на события после инициализации элементов
                dpDateFrom.SelectedDateChanged += DpDateFrom_SelectedDateChanged;
                dpDateTo.SelectedDateChanged += DpDateTo_SelectedDateChanged;

                cmbStatus.SelectionChanged += CmbStatus_SelectionChanged;
                cmbRequestType.SelectionChanged += CmbRequestType_SelectionChanged;
                cmbEmployee.SelectionChanged += CmbEmployee_SelectionChanged;
                txtSearch.TextChanged += TxtSearch_TextChanged;

                // Загружаем данные
                LoadRequestTypes();
                LoadEmployees();
                LoadRequests();

                // Устанавливаем активную кнопку "Все"
                SetActiveFilterButton(btnAll);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRequestTypes()
        {
            try
            {
                if (cmbRequestType == null) return;

                requestTypes.Clear();
                cmbRequestType.Items.Clear();
                cmbRequestType.Items.Add(new ComboBoxItem { Content = "Все типы" });

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT DISTINCT request_type 
                        FROM ServiceRequests 
                        WHERE request_type IS NOT NULL 
                        ORDER BY request_type";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var type = new RequestType
                            {
                                TypeName = reader["request_type"].ToString()
                            };
                            requestTypes.Add(type);
                            cmbRequestType.Items.Add(new ComboBoxItem
                            {
                                Content = type.TypeName
                            });
                        }
                    }
                }

                cmbRequestType.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов заявок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEmployees()
        {
            try
            {
                if (cmbEmployee == null) return;

                employees.Clear();
                cmbEmployee.Items.Clear();
                cmbEmployee.Items.Add(new ComboBoxItem { Content = "Все исполнители" });

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT e.employee_id, e.full_name, p.position_name
                        FROM Employees e
                        LEFT JOIN Positions p ON e.position_id = p.position_id
                        ORDER BY e.full_name";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var employee = new Employee
                            {
                                EmployeeId = Convert.ToInt32(reader["employee_id"]),
                                FullName = reader["full_name"].ToString(),
                                Position = reader["position_name"]?.ToString() ?? "Не указано"
                            };
                            employees.Add(employee);
                            cmbEmployee.Items.Add(new ComboBoxItem
                            {
                                Content = $"{employee.FullName} ({employee.Position})"
                            });
                        }
                    }
                }

                cmbEmployee.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки исполнителей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRequests()
        {
            try
            {
                allRequests.Clear();

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Основной запрос для получения заявок
                    string query = @"
                        SELECT 
                            sr.request_id,
                            sr.request_type,
                            sr.description,
                            sr.created_date,
                            sr.status,
                            sr.employee_id,
                            a.apartment_id,
                            a.apartment_number,
                            b.building_id,
                            b.address,
                            e.full_name as employee_name,
                            p.position_name,
                            ISNULL(re.total_expenses, 0) as total_expenses
                        FROM ServiceRequests sr
                        INNER JOIN Apartments a ON sr.apartment_id = a.apartment_id
                        INNER JOIN Buildings b ON a.building_id = b.building_id
                        LEFT JOIN Employees e ON sr.employee_id = e.employee_id
                        LEFT JOIN Positions p ON e.position_id = p.position_id
                        LEFT JOIN (
                            SELECT request_id, SUM(amount) as total_expenses
                            FROM RequestExpenses
                            GROUP BY request_id
                        ) re ON sr.request_id = re.request_id
                        ORDER BY sr.created_date DESC";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var request = new RequestDisplayItem
                            {
                                request_id = Convert.ToInt32(reader["request_id"]),
                                request_type = reader["request_type"].ToString(),
                                description = reader["description"].ToString(),
                                created_date = reader["created_date"] as DateTime?,
                                status = reader["status"].ToString(),
                                TotalExpenses = Convert.ToDecimal(reader["total_expenses"]),
                                DurationText = CalculateDuration(reader["created_date"] as DateTime?),
                                Apartment = new ApartmentInfo
                                {
                                    apartment_number = Convert.ToInt32(reader["apartment_number"]),
                                    Building = new BuildingInfo
                                    {
                                        address = reader["address"].ToString()
                                    }
                                }
                            };

                            if (reader["employee_id"] != DBNull.Value)
                            {
                                request.Employee = new EmployeeInfo
                                {
                                    full_name = reader["employee_name"].ToString(),
                                    position = reader["position_name"].ToString()
                                };
                            }

                            allRequests.Add(request);
                        }
                    }
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string CalculateDuration(DateTime? createdDate)
        {
            if (!createdDate.HasValue) return "Неизвестно";

            var duration = DateTime.Now - createdDate.Value;

            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays}д {duration.Hours}ч";
            else if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours}ч {duration.Minutes}м";
            else
                return $"{duration.Minutes}м";
        }

        private void ApplyFilters()
        {
            try
            {
                // Проверяем, что элементы инициализированы
                if (dgHistory == null || allRequests == null || allRequests.Count == 0)
                {
                    return;
                }

                var filtered = allRequests.AsEnumerable();

                // Фильтр по датам
                if (dpDateFrom != null && dpDateFrom.SelectedDate.HasValue)
                    filtered = filtered.Where(r => r.created_date >= dpDateFrom.SelectedDate.Value);

                if (dpDateTo != null && dpDateTo.SelectedDate.HasValue)
                {
                    var endDate = dpDateTo.SelectedDate.Value.AddDays(1);
                    filtered = filtered.Where(r => r.created_date < endDate);
                }

                // Фильтр по статусу
                if (cmbStatus != null && cmbStatus.SelectedIndex > 0)
                {
                    var statusItem = cmbStatus.SelectedItem as ComboBoxItem;
                    if (statusItem != null)
                    {
                        var status = statusItem.Content?.ToString();
                        if (!string.IsNullOrEmpty(status))
                        {
                            filtered = filtered.Where(r => r.status == status);
                        }
                    }
                }

                // Фильтр по типу заявки
                if (cmbRequestType != null && cmbRequestType.SelectedIndex > 0)
                {
                    var typeItem = cmbRequestType.SelectedItem as ComboBoxItem;
                    if (typeItem != null)
                    {
                        var type = typeItem.Content?.ToString();
                        if (!string.IsNullOrEmpty(type))
                        {
                            filtered = filtered.Where(r => r.request_type == type);
                        }
                    }
                }

                // Фильтр по исполнителю
                if (cmbEmployee != null && cmbEmployee.SelectedIndex > 0)
                {
                    var employeeItem = cmbEmployee.SelectedItem as ComboBoxItem;
                    if (employeeItem != null)
                    {
                        var employeeContent = employeeItem.Content?.ToString();
                        if (!string.IsNullOrEmpty(employeeContent))
                        {
                            var parts = employeeContent.Split('(');
                            if (parts.Length > 0)
                            {
                                var employeeName = parts[0].Trim();
                                filtered = filtered.Where(r =>
                                    r.Employee != null &&
                                    r.Employee.full_name.Contains(employeeName));
                            }
                        }
                    }
                }

                // Поиск
                if (txtSearch != null && !string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    var searchText = txtSearch.Text.ToLower();
                    filtered = filtered.Where(r =>
                        r.request_id.ToString().Contains(searchText) ||
                        (r.description != null && r.description.ToLower().Contains(searchText)) ||
                        (r.request_type != null && r.request_type.ToLower().Contains(searchText)) ||
                        (r.Apartment != null && r.Apartment.apartment_number.ToString().Contains(searchText)) ||
                        (r.Apartment != null && r.Apartment.Building != null &&
                         r.Apartment.Building.address != null &&
                         r.Apartment.Building.address.ToLower().Contains(searchText)) ||
                        (r.Employee != null && r.Employee.full_name != null &&
                         r.Employee.full_name.ToLower().Contains(searchText))
                    );
                }

                // Применяем фильтр по периоду
                var activeFilter = GetActiveFilterButton();
                filtered = ApplyPeriodFilter(filtered, activeFilter);

                // Обновляем DataGrid
                var filteredList = new ObservableCollection<RequestDisplayItem>(filtered);
                dgHistory.ItemsSource = filteredList;

                // Обновляем статистику
                UpdateStatistics(filteredList.ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка применения фильтров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private IEnumerable<RequestDisplayItem> ApplyPeriodFilter(IEnumerable<RequestDisplayItem> requests, Button activeFilter)
        {
            if (activeFilter == null) return requests;

            var today = DateTime.Today;
            var filterName = activeFilter.Name;

            switch (filterName)
            {
                case "btnToday":
                    return requests.Where(r => r.created_date?.Date == today);
                case "btnWeek":
                    var weekStart = today.AddDays(-(int)today.DayOfWeek);
                    return requests.Where(r => r.created_date >= weekStart);
                case "btnMonth":
                    var monthStart = new DateTime(today.Year, today.Month, 1);
                    return requests.Where(r => r.created_date >= monthStart);
                case "btnQuarter":
                    var quarter = (today.Month - 1) / 3 + 1;
                    var quarterStart = new DateTime(today.Year, (quarter - 1) * 3 + 1, 1);
                    return requests.Where(r => r.created_date >= quarterStart);
                case "btnYear":
                    var yearStart = new DateTime(today.Year, 1, 1);
                    return requests.Where(r => r.created_date >= yearStart);
                default: // btnAll
                    return requests;
            }
        }

        private void UpdateStatistics(List<RequestDisplayItem> requests)
        {
            try
            {
                int total = requests.Count;
                int completed = requests.Count(r => r.status == "Выполнена" || r.status == "Закрыта");
                int inProgress = requests.Count(r => r.status == "В работе");
                int open = requests.Count(r => r.status == "Открыта");

                txtTotalRequests.Text = $"Всего: {total}";
                txtCompletedRequests.Text = $"Выполнено: {completed}";
                txtInProgressRequests.Text = $"В работе: {inProgress}";
                txtOpenRequests.Text = $"Открыто: {open}";

                // Расчет среднего времени
                if (completed > 0)
                {
                    try
                    {
                        var durations = requests
                            .Where(r => !string.IsNullOrEmpty(r.DurationText))
                            .Select(r =>
                            {
                                var durationText = r.DurationText;
                                double totalHours = 0;

                                if (durationText.Contains("д"))
                                {
                                    var dayParts = durationText.Split('д');
                                    if (dayParts.Length > 0 && double.TryParse(dayParts[0], out double days))
                                    {
                                        totalHours = days * 24;
                                        durationText = dayParts.Length > 1 ? dayParts[1] : "";
                                    }
                                }

                                if (durationText.Contains("ч"))
                                {
                                    var hourParts = durationText.Split('ч');
                                    if (hourParts.Length > 0 && double.TryParse(hourParts[0], out double hours))
                                    {
                                        totalHours += hours;
                                    }
                                }

                                return totalHours;
                            })
                            .Where(h => h > 0)
                            .ToList();

                        if (durations.Any())
                        {
                            var avgHours = durations.Average();
                            txtAvgDuration.Text = $"Ср. время: {avgHours:F1} ч";
                        }
                        else
                        {
                            txtAvgDuration.Text = "Ср. время: -";
                        }
                    }
                    catch
                    {
                        txtAvgDuration.Text = "Ср. время: -";
                    }
                }
                else
                {
                    txtAvgDuration.Text = "Ср. время: -";
                }

                // Статистика по исполнителям
                var employeeStats = requests
                    .Where(r => r.Employee != null)
                    .GroupBy(r => r.Employee.full_name)
                    .Select(g => new EmployeeStatistic
                    {
                        EmployeeName = g.Key,
                        Total = g.Count(),
                        Completed = g.Count(r => r.status == "Выполнена" || r.status == "Закрыта")
                    })
                    .Select(s => new
                    {
                        EmployeeName = s.EmployeeName,
                        Stats = $"{s.Total} заявка(ок), {(s.Total > 0 ? (s.Completed * 100 / s.Total) : 0)}% выполнено"
                    })
                    .ToList();

                lvEmployeeStats.ItemsSource = employeeStats;

                // Статистика по типам заявок
                var typeStats = requests
                    .GroupBy(r => r.request_type)
                    .Select(g => new
                    {
                        Type = g.Key,
                        Count = $"{g.Count()} заявка(ок)"
                    })
                    .ToList();

                lvTypeStats.ItemsSource = typeStats;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления статистики: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetActiveFilterButton(Button activeButton)
        {
            try
            {
                if (activeButton == null) return;

                var filterStyle = FindResource("FilterButtonStyle") as Style;
                var activeStyle = FindResource("ActiveFilterButtonStyle") as Style;

                if (filterStyle == null || activeStyle == null) return;

                // Сбрасываем все кнопки
                if (btnToday != null) btnToday.Style = filterStyle;
                if (btnWeek != null) btnWeek.Style = filterStyle;
                if (btnMonth != null) btnMonth.Style = filterStyle;
                if (btnQuarter != null) btnQuarter.Style = filterStyle;
                if (btnYear != null) btnYear.Style = filterStyle;
                if (btnAll != null) btnAll.Style = filterStyle;

                // Устанавливаем активную
                activeButton.Style = activeStyle;

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка установки фильтра: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Button GetActiveFilterButton()
        {
            try
            {
                if (btnAll == null || btnToday == null || btnWeek == null ||
                    btnMonth == null || btnQuarter == null || btnYear == null)
                    return btnAll;

                var activeButtonStyle = FindResource("ActiveFilterButtonStyle") as Style;
                if (activeButtonStyle == null) return btnAll;

                if (btnAll.Style == activeButtonStyle)
                    return btnAll;
                else if (btnToday.Style == activeButtonStyle)
                    return btnToday;
                else if (btnWeek.Style == activeButtonStyle)
                    return btnWeek;
                else if (btnMonth.Style == activeButtonStyle)
                    return btnMonth;
                else if (btnQuarter.Style == activeButtonStyle)
                    return btnQuarter;
                else if (btnYear.Style == activeButtonStyle)
                    return btnYear;

                return btnAll;
            }
            catch
            {
                return btnAll;
            }
        }

        // Обработчики фильтров по периоду
        private void BtnToday_Click(object sender, RoutedEventArgs e) => SetActiveFilterButton(btnToday);
        private void BtnWeek_Click(object sender, RoutedEventArgs e) => SetActiveFilterButton(btnWeek);
        private void BtnMonth_Click(object sender, RoutedEventArgs e) => SetActiveFilterButton(btnMonth);
        private void BtnQuarter_Click(object sender, RoutedEventArgs e) => SetActiveFilterButton(btnQuarter);
        private void BtnYear_Click(object sender, RoutedEventArgs e) => SetActiveFilterButton(btnYear);
        private void BtnAll_Click(object sender, RoutedEventArgs e) => SetActiveFilterButton(btnAll);

        // Другие обработчики фильтров
        private void BtnApplyFilters_Click(object sender, RoutedEventArgs e) => ApplyFilters();

        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            if (dpDateFrom != null)
                dpDateFrom.SelectedDate = DateTime.Today.AddMonths(-1);
            if (dpDateTo != null)
                dpDateTo.SelectedDate = DateTime.Today;
            if (cmbStatus != null)
                cmbStatus.SelectedIndex = 0;
            if (cmbRequestType != null)
                cmbRequestType.SelectedIndex = 0;
            if (cmbEmployee != null)
                cmbEmployee.SelectedIndex = 0;
            if (txtSearch != null)
                txtSearch.Text = "";
            SetActiveFilterButton(btnAll);
        }

        private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void CmbRequestType_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void CmbEmployee_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();

        // Обработчики событий DatePicker
        private void DpDateFrom_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DpDateTo_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        // Просмотр деталей заявки
        private void BtnViewDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var request = button.DataContext as RequestDisplayItem;
            if (request == null) return;

            ShowRequestDetails(request);
        }

        private void DgHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgHistory.SelectedItem is RequestDisplayItem selectedRequest)
            {
                ShowRequestDetails(selectedRequest);
            }
        }

        private void ShowRequestDetails(RequestDisplayItem request)
        {
            try
            {
                spRequestDetails.Children.Clear();
                txtDetailsTitle.Text = $"Детали заявки №{request.request_id}";

                AddDetail("📋 Номер заявки:", request.request_id.ToString());

                if (request.Apartment != null)
                {
                    AddDetail("🏠 Квартира:", $"{request.Apartment.apartment_number} ({request.Apartment.Building.address})");
                }

                AddDetail("🔧 Тип заявки:", request.request_type);
                AddDetail("📝 Описание:", request.description);
                AddDetail("📅 Дата создания:", request.created_date?.ToString("dd.MM.yyyy HH:mm"));
                AddDetail("🎯 Статус:", request.status);

                if (request.Employee != null)
                {
                    AddDetail("👨‍🔧 Исполнитель:", $"{request.Employee.full_name} ({request.Employee.position})");
                }

                AddDetail("⏱️ Длительность:", request.DurationText);
                AddDetail("💰 Общие расходы:", $"{request.TotalExpenses:N2} руб");

                pnlRequestDetails.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddDetail(string label, string value)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0)
            };
            stackPanel.Children.Add(new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.SemiBold,
                Width = 150
            });
            stackPanel.Children.Add(new TextBlock
            {
                Text = value,
                TextWrapping = TextWrapping.Wrap
            });
            spRequestDetails.Children.Add(stackPanel);
        }

        private void BtnCloseDetails_Click(object sender, RoutedEventArgs e)
        {
            pnlRequestDetails.Visibility = Visibility.Collapsed;
        }

        // Хронология заявок
        private void BtnViewTimeline_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedRequest = dgHistory.SelectedItem as RequestDisplayItem;
                if (selectedRequest == null)
                {
                    MessageBox.Show("Выберите заявку для просмотра хронологии", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                ShowTimeline(selectedRequest.request_id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки хронологии: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowTimeline(int requestId)
        {
            try
            {
                var timelineWindow = new Window
                {
                    Title = $"Хронология заявки №{requestId}",
                    Width = 600,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this)
                };

                var stackPanel = new StackPanel();
                stackPanel.Margin = new Thickness(10);

                // Заголовок
                stackPanel.Children.Add(new TextBlock
                {
                    Text = $"📅 Хронология выполнения заявки №{requestId}",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                // Загрузка данных хронологии
                var timelineItems = LoadTimelineData(requestId);

                if (timelineItems.Any())
                {
                    foreach (var item in timelineItems)
                    {
                        var itemPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness(0, 5, 0, 5)
                        };

                        if (item.IsCurrent)
                        {
                            itemPanel.Background = Brushes.LightGreen;
                        }

                        // Добавляем Margin внутренних элементов
                        var innerMargin = new Thickness(5, 5, 5, 5);

                        // Иконка
                        string icon;
                        switch (item.EventType)
                        {
                            case "Создание":
                                icon = "📝";
                                break;
                            case "Назначение":
                                icon = "👤";
                                break;
                            case "Изменение статуса":
                                icon = "🔄";
                                break;
                            case "Расход":
                                icon = "💰";
                                break;
                            case "Комментарий":
                                icon = "💬";
                                break;
                            case "Завершение":
                                icon = "✅";
                                break;
                            default:
                                icon = "⚫";
                                break;
                        }

                        var iconTextBlock = new TextBlock
                        {
                            Text = icon,
                            FontSize = 16,
                            Width = 30,
                            Margin = innerMargin
                        };
                        itemPanel.Children.Add(iconTextBlock);

                        // Основная информация
                        var infoPanel = new StackPanel();
                        var descriptionTextBlock = new TextBlock
                        {
                            Text = item.Description,
                            FontWeight = FontWeights.SemiBold,
                            Margin = innerMargin
                        };
                        infoPanel.Children.Add(descriptionTextBlock);

                        var timestampTextBlock = new TextBlock
                        {
                            Text = item.Timestamp.ToString("dd.MM.yyyy HH:mm"),
                            FontSize = 11,
                            Foreground = Brushes.Gray,
                            Margin = innerMargin
                        };
                        infoPanel.Children.Add(timestampTextBlock);

                        if (!string.IsNullOrEmpty(item.Responsible))
                        {
                            var responsibleTextBlock = new TextBlock
                            {
                                Text = $"Ответственный: {item.Responsible}",
                                FontSize = 11,
                                Foreground = Brushes.DarkBlue,
                                Margin = innerMargin
                            };
                            infoPanel.Children.Add(responsibleTextBlock);
                        }

                        itemPanel.Children.Add(infoPanel);
                        stackPanel.Children.Add(itemPanel);
                    }
                }
                else
                {
                    stackPanel.Children.Add(new TextBlock
                    {
                        Text = "Нет данных хронологии для этой заявки",
                        FontStyle = FontStyles.Italic,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 20, 0, 0)
                    });
                }

                // Кнопка закрытия
                var closeButton = new Button
                {
                    Content = "Закрыть",
                    Margin = new Thickness(0, 20, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Style = FindResource("PrimaryButtonStyle") as Style
                };

                // Устанавливаем Padding через Thickness
                closeButton.Padding = new Thickness(20, 5, 20, 5);

                closeButton.Click += (s, ev) => timelineWindow.Close();

                stackPanel.Children.Add(closeButton);

                timelineWindow.Content = new ScrollViewer
                {
                    Content = stackPanel,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };

                timelineWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отображения хронологии: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<TimelineItem> LoadTimelineData(int requestId)
        {
            var timelineItems = new List<TimelineItem>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // 1. Событие создания заявки
                    string query = @"
                        SELECT 
                            'Создание' as event_type,
                            created_date as timestamp,
                            'Заявка создана' as description,
                            NULL as responsible,
                            NULL as amount,
                            1 as is_current
                        FROM ServiceRequests
                        WHERE request_id = @requestId

                        UNION ALL

                        -- 2. Изменения статуса
                        SELECT 
                            'Изменение статуса' as event_type,
                            created_date as timestamp,
                            'Статус изменен на: ' + status as description,
                            e.full_name as responsible,
                            NULL as amount,
                            CASE WHEN status = 'Выполнена' THEN 1 ELSE 0 END as is_current
                        FROM ServiceRequests sr
                        LEFT JOIN Employees e ON sr.employee_id = e.employee_id
                        WHERE sr.request_id = @requestId

                        UNION ALL

                        -- 3. Расходы по заявке
                        SELECT 
                            'Расход' as event_type,
                            expense_date as timestamp,
                            expense_type + ': ' + ISNULL(description, '') as description,
                            created_by as responsible,
                            amount,
                            0 as is_current
                        FROM RequestExpenses
                        WHERE request_id = @requestId
                        ORDER BY timestamp";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@requestId", requestId);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                timelineItems.Add(new TimelineItem
                                {
                                    EventType = reader["event_type"].ToString(),
                                    Timestamp = Convert.ToDateTime(reader["timestamp"]),
                                    Description = reader["description"].ToString(),
                                    Responsible = reader["responsible"]?.ToString(),
                                    Amount = reader["amount"] != DBNull.Value ? Convert.ToDecimal(reader["amount"]) : 0,
                                    IsCurrent = Convert.ToBoolean(reader["is_current"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки хронологии: {ex.Message}");
            }

            return timelineItems.OrderBy(t => t.Timestamp).ToList();
        }

        private void BtnViewExpenses_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedRequest = dgHistory.SelectedItem as RequestDisplayItem;
                if (selectedRequest == null)
                {
                    MessageBox.Show("Выберите заявку для просмотра расходов", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                ShowExpenses(selectedRequest.request_id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки расходов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowExpenses(int requestId)
        {
            try
            {
                var expensesWindow = new Window
                {
                    Title = $"Расходы по заявке №{requestId}",
                    Width = 500,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this)
                };

                var dataGrid = new DataGrid
                {
                    AutoGenerateColumns = false,
                    IsReadOnly = true,
                    Margin = new Thickness(10)
                };

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Тип расхода",
                    Binding = new Binding("ExpenseType"),
                    Width = 100
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Описание",
                    Binding = new Binding("Description"),
                    Width = 200
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Сумма",
                    Binding = new Binding("Amount") { StringFormat = "N2" },
                    Width = 80
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Дата",
                    Binding = new Binding("ExpenseDate") { StringFormat = "dd.MM.yyyy" },
                    Width = 80
                });

                // Загрузка расходов
                var expenses = LoadExpenses(requestId);
                dataGrid.ItemsSource = expenses;

                var totalAmount = expenses.Sum(e => e.Amount);

                var stackPanel = new StackPanel();
                stackPanel.Children.Add(new TextBlock
                {
                    Text = $"💰 Расходы по заявке №{requestId}",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(10, 10, 10, 10)
                });

                stackPanel.Children.Add(new TextBlock
                {
                    Text = $"Общая сумма: {totalAmount:N2} руб",
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.DarkGreen,
                    Margin = new Thickness(10, 0, 10, 10)
                });

                stackPanel.Children.Add(dataGrid);

                var closeButton = new Button
                {
                    Content = "Закрыть",
                    Margin = new Thickness(10),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Style = FindResource("PrimaryButtonStyle") as Style
                };

                closeButton.Padding = new Thickness(20, 5, 20, 5);
                closeButton.Click += (s, ev) => expensesWindow.Close();

                stackPanel.Children.Add(closeButton);

                expensesWindow.Content = stackPanel;
                expensesWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отображения расходов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<ExpenseItem> LoadExpenses(int requestId)
        {
            var expenses = new List<ExpenseItem>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT 
                            expense_type,
                            description,
                            amount,
                            expense_date,
                            created_by
                        FROM RequestExpenses
                        WHERE request_id = @requestId
                        ORDER BY expense_date";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@requestId", requestId);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                expenses.Add(new ExpenseItem
                                {
                                    ExpenseType = reader["expense_type"].ToString(),
                                    Description = reader["description"].ToString(),
                                    Amount = Convert.ToDecimal(reader["amount"]),
                                    ExpenseDate = Convert.ToDateTime(reader["expense_date"]),
                                    CreatedBy = reader["created_by"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки расходов: {ex.Message}");
            }

            return expenses;
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv",
                    DefaultExt = "csv",
                    FileName = $"Заявки_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportToCsv(saveDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCsv(string filePath)
        {
            var requests = dgHistory.ItemsSource as IEnumerable<RequestDisplayItem>;
            if (requests == null) return;

            using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                // Заголовки
                writer.WriteLine("№;Дата;Квартира;Адрес;Тип;Описание;Статус;Исполнитель;Длительность;Расходы");

                // Данные
                foreach (var request in requests)
                {
                    var apartmentNumber = request.Apartment?.apartment_number.ToString() ?? "";
                    var address = request.Apartment?.Building?.address ?? "";
                    var employeeName = request.Employee?.full_name ?? "";

                    writer.WriteLine($"{request.request_id};" +
                                   $"{request.created_date:dd.MM.yyyy HH:mm};" +
                                   $"{apartmentNumber};" +
                                   $"{address};" +
                                   $"{request.request_type};" +
                                   $"{request.description};" +
                                   $"{request.status};" +
                                   $"{employeeName};" +
                                   $"{request.DurationText};" +
                                   $"{request.TotalExpenses:N2}");
                }
            }

            MessageBox.Show($"Данные экспортированы в файл: {filePath}", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(this, "Печать истории заявок");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка печати: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Классы для данных
    public class RequestDisplayItem
    {
        public int request_id { get; set; }
        public string request_type { get; set; }
        public string description { get; set; }
        public DateTime? created_date { get; set; }
        public string status { get; set; }
        public string DurationText { get; set; }
        public decimal TotalExpenses { get; set; }
        public ApartmentInfo Apartment { get; set; }
        public EmployeeInfo Employee { get; set; }
    }

    public class ApartmentInfo
    {
        public int apartment_number { get; set; }
        public BuildingInfo Building { get; set; }
    }

    public class BuildingInfo
    {
        public string address { get; set; }
    }

    public class EmployeeInfo
    {
        public string full_name { get; set; }
        public string position { get; set; }
    }

    public class RequestType
    {
        public string TypeName { get; set; }
    }

    public class Employee
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; }
        public string Position { get; set; }
    }

    public class EmployeeStatistic
    {
        public string EmployeeName { get; set; }
        public int Total { get; set; }
        public int Completed { get; set; }
    }

    public class TimelineItem
    {
        public string EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
        public string Responsible { get; set; }
        public decimal Amount { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class ExpenseItem
    {
        public string ExpenseType { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string CreatedBy { get; set; }
    }

    // Конвертеры
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status)
                {
                    case "Открыта": return System.Windows.Media.Brushes.Red;
                    case "В работе": return System.Windows.Media.Brushes.Orange;
                    case "Выполнена":
                    case "Закрыта": return System.Windows.Media.Brushes.Green;
                    default: return System.Windows.Media.Brushes.Blue;
                }
            }
            return System.Windows.Media.Brushes.Blue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("dd.MM.yy HH:mm");
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}