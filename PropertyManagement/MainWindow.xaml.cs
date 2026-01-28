using PropertyManagement.Pages;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

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

            LoadUserInfo();
            SetupInterface();

            // Переходим на главную страницу
            MainFrame.Navigate(new MainPage());
        }

        private void LoadUserInfo()
        {
            // Получаем данные пользователя
            string fullName = Application.Current.Properties["FullName"] as string;
            string positionName = Application.Current.Properties["PositionName"] as string;

            // Устанавливаем заголовок
            Title = $"Управляющая компания - {fullName}";

            // Отображаем информацию о пользователе
            txtCurrentUser.Text = fullName;
            txtUserPosition.Text = positionName;
        }

        private void SetupInterface()
        {
            string positionName = Application.Current.Properties["PositionName"] as string;
            if (string.IsNullOrEmpty(positionName))
                return;

            string positionLower = positionName.ToLower();

            // Администратор - полный доступ
            if (positionLower.Contains("админ") || positionLower.Contains("administrator"))
            {
                // Все элементы видимы (оставляем как есть)
            }
            // Руководитель - почти полный доступ
            else if (positionLower.Contains("руковод") || positionLower.Contains("директор") ||
                     positionLower.Contains("manager"))
            {
                btnSettings.Visibility = Visibility.Collapsed;
            }
            // Бухгалтер - финансовые операции
            else if (positionLower.Contains("бухгал") || positionLower.Contains("accountant") ||
                     positionLower.Contains("финанс"))
            {
                btnBuildings.Visibility = Visibility.Collapsed;
                btnApartments.Visibility = Visibility.Collapsed;
                btnEmployees.Visibility = Visibility.Collapsed;
                btnServiceRequests.Visibility = Visibility.Collapsed;
                btnNewRequest.Visibility = Visibility.Collapsed;
                btnSettings.Visibility = Visibility.Collapsed;
            }
            // Техник - работа с заявками
            else if (positionLower.Contains("техник") || positionLower.Contains("technician") ||
                     positionLower.Contains("мастер") || positionLower.Contains("электрик") ||
                     positionLower.Contains("сантехник"))
            {
                btnBuildings.Visibility = Visibility.Collapsed;
                btnApartments.Visibility = Visibility.Collapsed;
                btnOwners.Visibility = Visibility.Collapsed;
                btnEmployees.Visibility = Visibility.Collapsed;
                btnRequestExpenses.Visibility = Visibility.Collapsed;
                btnNewRequest.Visibility = Visibility.Collapsed;
                btnReports.Visibility = Visibility.Collapsed;
                btnSettings.Visibility = Visibility.Collapsed;
            }
            // Диспетчер - прием заявок
            else if (positionLower.Contains("диспетчер") || positionLower.Contains("dispatcher"))
            {
                btnBuildings.Visibility = Visibility.Collapsed;
                btnEmployees.Visibility = Visibility.Collapsed;
                btnRequestExpenses.Visibility = Visibility.Collapsed;
                btnReports.Visibility = Visibility.Collapsed;
                btnSettings.Visibility = Visibility.Collapsed;
            }
            // Обычный пользователь
            else
            {
                btnEmployees.Visibility = Visibility.Collapsed;
                btnRequestExpenses.Visibility = Visibility.Collapsed;
                btnNewRequest.Visibility = Visibility.Collapsed;
                btnReports.Visibility = Visibility.Collapsed;
                btnSettings.Visibility = Visibility.Collapsed;
            }

            // Скрываем разделы, если все кнопки в них скрыты
            UpdateSectionVisibility();
        }

        private void UpdateSectionVisibility()
        {
            // Проверяем видимость кнопок в каждом разделе
            bool hasVisibleDataButtons =
                btnBuildings.Visibility == Visibility.Visible ||
                btnApartments.Visibility == Visibility.Visible ||
                btnOwners.Visibility == Visibility.Visible ||
                btnEmployees.Visibility == Visibility.Visible ||
                btnRequestExpenses.Visibility == Visibility.Visible;

            bool hasVisibleRequestButtons =
                btnServiceRequests.Visibility == Visibility.Visible ||
                btnNewRequest.Visibility == Visibility.Visible;

            bool hasVisibleReportButtons =
                btnReports.Visibility == Visibility.Visible;

            bool hasVisibleAdditionalButtons =
                btnSettings.Visibility == Visibility.Visible ||
                btnHelp.Visibility == Visibility.Visible;

            // Обновляем видимость разделов
            sectionData.Visibility = hasVisibleDataButtons ? Visibility.Visible : Visibility.Collapsed;
            sectionRequests.Visibility = hasVisibleRequestButtons ? Visibility.Visible : Visibility.Collapsed;
            sectionReports.Visibility = hasVisibleReportButtons ? Visibility.Visible : Visibility.Collapsed;
            sectionAdditional.Visibility = hasVisibleAdditionalButtons ? Visibility.Visible : Visibility.Collapsed;
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

        // Обработчики навигации

        private void MainPage_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new MainPage());
        }

        private void ReportsPage_Click(object sender, RoutedEventArgs e)
        {
            if (btnReports.Visibility == Visibility.Visible)
                MainFrame.Navigate(new ReportsPage());
            else
                ShowAccessDenied();
        }

        private void RequestExpensesPage_Click(object sender, RoutedEventArgs e)
        {
            if (btnRequestExpenses.Visibility == Visibility.Visible)
                MainFrame.Navigate(new RequestExpensesPage());
            else
                ShowAccessDenied();
        }

        private void BuildingsPage_Click(object sender, RoutedEventArgs e)
        {
            if (btnBuildings.Visibility == Visibility.Visible)
                MainFrame.Navigate(new BuildingsPage());
            else
                ShowAccessDenied();
        }

        private void ApartmentsPage_Click(object sender, RoutedEventArgs e)
        {
            if (btnApartments.Visibility == Visibility.Visible)
                MainFrame.Navigate(new ApartmentsPage());
            else
                ShowAccessDenied();
        }

        private void OwnersPage_Click(object sender, RoutedEventArgs e)
        {
            if (btnOwners.Visibility == Visibility.Visible)
                MainFrame.Navigate(new OwnersPage());
            else
                ShowAccessDenied();
        }

        private void EmployeesPage_Click(object sender, RoutedEventArgs e)
        {
            if (btnEmployees.Visibility == Visibility.Visible)
                MainFrame.Navigate(new EmployeesPage());
            else
                ShowAccessDenied();
        }

        private void ServiceRequestsPage_Click(object sender, RoutedEventArgs e)
        {
            if (btnServiceRequests.Visibility == Visibility.Visible)
                MainFrame.Navigate(new ServiceRequestsPage());
            else
                ShowAccessDenied();
        }

        private void NewRequestPage_Click(object sender, RoutedEventArgs e)
        {
            if (btnNewRequest.Visibility == Visibility.Visible)
                MainFrame.Navigate(new ServiceRequestEditPage());
            else
                ShowAccessDenied();
        }

        private void SettingsPage_Click(object sender, RoutedEventArgs e)
        {
            if (btnSettings.Visibility == Visibility.Visible)
                MessageBox.Show("Страница 'Настройки' в разработке", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            else
                ShowAccessDenied();
        }

        private void HelpPage_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Страница 'Справка' в разработке", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowAccessDenied()
        {
            MessageBox.Show("Доступ запрещен. У вас нет прав для выполнения этого действия.",
                "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Статические методы для получения данных пользователя
        public static int? GetCurrentEmployeeId()
        {
            return Application.Current.Properties["EmployeeId"] as int?;
        }

        public static string GetCurrentFullName()
        {
            return Application.Current.Properties["FullName"] as string;
        }

        public static string GetCurrentPositionName()
        {
            return Application.Current.Properties["PositionName"] as string;
        }
    }
}