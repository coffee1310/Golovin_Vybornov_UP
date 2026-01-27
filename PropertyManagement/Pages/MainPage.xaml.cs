using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PropertyManagement.Pages
{
    public partial class MainPage : Page
    {
        private readonly PropertyManagementEntities _context;

        public MainPage()
        {
            InitializeComponent();
            _context = new PropertyManagementEntities();

            // Раскомментируйте для проверки структуры
            // CheckServiceRequestsStructure();

            LoadStatistics();
            LoadRecentRequests();
        }

        private void LoadStatistics()
        {
            try
            {
                // Загрузка статистики из базы данных
                var buildingsCount = _context.Buildings.Count();
                var apartmentsCount = _context.Apartments.Count();
                var employeesCount = _context.Employees.Count();
                var requestsCount = _context.ServiceRequests.Count();

                BuildingsCount.Text = buildingsCount.ToString();
                ApartmentsCount.Text = apartmentsCount.ToString();
                EmployeesCount.Text = employeesCount.ToString();
                RequestsCount.Text = requestsCount.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                // Устанавливаем значения по умолчанию
                BuildingsCount.Text = "0";
                ApartmentsCount.Text = "0";
                EmployeesCount.Text = "0";
                RequestsCount.Text = "0";
            }
        }

        private void LoadRecentRequests()
        {
            try
            {
                // Сначала получим все свойства ServiceRequests
                var properties = typeof(ServiceRequests).GetProperties();
                
                // Если есть поле с датой создания, используем его
                var dateProperty = properties.FirstOrDefault(p => 
                    p.Name.Contains("Date") || 
                    p.Name.Contains("Created") || 
                    p.PropertyType == typeof(DateTime) ||
                    p.PropertyType == typeof(DateTime?));
                
                IQueryable<ServiceRequests> query = _context.ServiceRequests;
                
                // Сортируем по дате, если найдено подходящее поле
                if (dateProperty != null)
                {
                    // Используем динамическое LINQ или простую сортировку
                    // Для простоты сначала возьмем без сортировки
                    query = query.OrderByDescending(r => r.request_id);
                }
                
                var recentRequests = query
                    .Take(5)
                    .Select(r => new
                    {
                        Id = r.request_id,
                        Type = r.request_type,
                        Desc = r.description,
                        State = r.status
                        // Добавьте другие поля по необходимости
                    })
                    .ToList();

                RecentRequestsGrid.ItemsSource = recentRequests;

                if (!recentRequests.Any())
                {
                    MessageBox.Show("В базе данных нет заявок", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Остальные методы...
        private void AddBuilding_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция 'Добавить здание' будет доступна на странице 'Здания'", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция 'Добавить сотрудника' будет доступна на странице 'Сотрудники'", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CreateRequest_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция 'Создать заявку' будет доступна на странице 'Новая заявка'", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewReports_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция 'Просмотреть отчеты' в разработке", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewAllRequests_Click(object sender, RoutedEventArgs e)
        {
            // Получаем главное окно
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                // Просто показываем сообщение, так как метода NavigateToServiceRequests нет
                //mainWindow.ServiceRequestsPage_Click(sender, e);
            }
        }

        private void CheckServiceRequestsStructure()
        {
            try
            {
                var firstRequest = _context.ServiceRequests.FirstOrDefault();
                if (firstRequest != null)
                {
                    var properties = firstRequest.GetType().GetProperties();
                    string propList = "Свойства ServiceRequests:\n";
                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(firstRequest);
                        propList += $"{prop.Name} ({prop.PropertyType.Name}) = {value}\n";
                    }
                    
                    MessageBox.Show(propList, "Структура таблицы ServiceRequests");
                }
                else
                {
                    MessageBox.Show("Таблица ServiceRequests пустая", "Информация");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки структуры: {ex.Message}", "Ошибка");
            }
        }
    }
}