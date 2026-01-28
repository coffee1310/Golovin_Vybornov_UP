using PropertyManagement.Pages;
using System.Windows;
using System.Windows.Controls;

namespace PropertyManagement
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Проверяем авторизацию
            if (Application.Current.Properties["EmployeeId"] == null)
            {
                MessageBox.Show("Ошибка авторизации", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            // Получаем данные пользователя
            int employeeId = (int)Application.Current.Properties["EmployeeId"];
            string fullName = Application.Current.Properties["FullName"] as string;
            string position = Application.Current.Properties["Position"] as string;

            // Устанавливаем заголовок с именем пользователя
            Title = $"Управляющая компания - {fullName}";

            // Переходим на главную страницу
            MainFrame.Navigate(new MainPage());
        }

        public static int? GetCurrentEmployeeId()
        {
            return Application.Current.Properties["EmployeeId"] as int?;
        }

        public static string GetCurrentFullName()
        {
            return Application.Current.Properties["FullName"] as string;
        }

        public static string GetCurrentPosition()
        {
            return Application.Current.Properties["Position"] as string;
        }

        // Метод для выхода
        private void Logout()
        {
            // Очищаем данные пользователя
            Application.Current.Properties.Remove("EmployeeId");
            Application.Current.Properties.Remove("FullName");
            Application.Current.Properties.Remove("Position");
            Application.Current.Properties.Remove("PositionId");
            Application.Current.Properties.Remove("Login");
            Application.Current.Properties.Remove("PositionName");

            // Показываем окно авторизации
            var loginWindow = new Window
            {
                Title = "Авторизация",
                Content = new LoginPage(),
                Width = 420,
                Height = 420,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            loginWindow.Show();
            this.Close();
        }

        // Обработчик кнопки выхода
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Logout();
        }

        private void MainPage_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new MainPage());
        }

        private void ReportsPage_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReportsPage());
        }

        private void RequestExpensesPage_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new RequestExpensesPage());
        }

        private void BuildingsPage_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new BuildingsPage());
        }

        private void ApartmentsPage_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ApartmentsPage());
        }

        private void OwnersPage_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new OwnersPage());
        }


        private void EmployeesPage_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new EmployeesPage());
        }

        private void ServiceRequestsPage_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ServiceRequestsPage());
        }

        private void NewRequestPage_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ServiceRequestEditPage());
        }

        private void SettingsPage_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Страница 'Настройки' в разработке", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void HelpPage_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Страница 'Справка' в разработке", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}