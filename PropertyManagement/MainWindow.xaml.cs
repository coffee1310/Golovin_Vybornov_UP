using PropertyManagement.Pages;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PropertyManagement
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Загружаем главную страницу при старте
            MainFrame.Navigate(new MainPage());
        }

        private void MainPage_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new MainPage());
        }

        private void BuildingsPage_Click(object sender, RoutedEventArgs e)
        {
            // Временная заглушка
            MessageBox.Show("Страница 'Здания' в разработке", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ApartmentsPage_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Страница 'Квартиры' в разработке", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OwnersPage_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Страница 'Собственники' в разработке", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EmployeesPage_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Страница 'Сотрудники' в разработке", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ServiceRequestsPage_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Страница 'Заявки' в разработке", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NewRequestPage_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Страница 'Новая заявка' в разработке", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
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