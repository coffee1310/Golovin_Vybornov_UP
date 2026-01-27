using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PropertyManagement.Pages
{
    public partial class BuildingsPage : Page
    {
        private readonly PropertyManagementEntities _context;
        private List<Buildings> _allBuildings;
        private Buildings _selectedBuilding;

        public BuildingsPage()
        {
            InitializeComponent();
            _context = new PropertyManagementEntities();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _allBuildings = _context.Buildings.ToList();
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
            var filtered = _allBuildings.AsEnumerable();

            // Поиск
            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                string search = SearchTextBox.Text.ToLower();
                filtered = filtered.Where(b =>
                    (b.address != null && b.address.ToLower().Contains(search)) ||
                    (b.city != null && b.city.ToLower().Contains(search)));
            }

            BuildingsGrid.ItemsSource = filtered.ToList();
            BuildingsCountText.Text = $" ({filtered.Count()} зданий)";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void AddBuilding_Click(object sender, RoutedEventArgs e)
        {
            // Открываем страницу добавления
            NavigationService.Navigate(new BuildingEditPage());
        }

        private void EditBuilding_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBuilding == null)
            {
                MessageBox.Show("Выберите здание для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Открываем страницу редактирования
            NavigationService.Navigate(new BuildingEditPage(_selectedBuilding.building_id));
        }

        private void DeleteBuilding_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBuilding == null)
            {
                MessageBox.Show("Выберите здание для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить здание: {_selectedBuilding.address}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _context.Buildings.Remove(_selectedBuilding);
                    _context.SaveChanges();

                    MessageBox.Show("Здание удалено", "Успех",
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

        private void BuildingsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedBuilding = BuildingsGrid.SelectedItem as Buildings;
        }
    }
}