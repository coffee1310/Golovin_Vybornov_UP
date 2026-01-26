using System;
using System.Data.Entity;
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
                // Загрузка последних заявок из базы данных (без Include)
                var recentRequests = _context.ServiceRequests
                    .OrderByDescending(r => r.request_id)
                    .Take(5)
                    .ToList();

                RecentRequestsGrid.ItemsSource = recentRequests;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
                // Переходим на страницу заявок
                //mainWindow.NavigateToServiceRequests();
            }
        }
    }
}