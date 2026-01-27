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
            MainFrame.Navigate(new MainPage());
        }

        private void MainPage_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new MainPage());
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