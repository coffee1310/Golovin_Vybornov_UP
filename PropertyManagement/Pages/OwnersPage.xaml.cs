using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PropertyManagement.Pages
{
    public partial class OwnersPage : Page
    {
        private readonly PropertyManagementEntities _context;
        private List<OwnerViewModel> _allOwners;
        private OwnerViewModel _selectedOwner;

        public class OwnerViewModel
        {
            public int owner_id { get; set; }
            public string full_name { get; set; }
            public string passport_data { get; set; }
            public string phone_number { get; set; }
            public string email { get; set; }
            public DateTime? registration_date { get; set; }
            public int apartments_count { get; set; }
        }

        public OwnersPage()
        {
            InitializeComponent();
            _context = new PropertyManagementEntities();
            LoadData();
            ApartmentFilterComboBox.SelectedIndex = 0;
        }

        private void LoadData()
        {
            try
            {
                // Загружаем всех собственников
                var owners = _context.Owners.ToList();

                // Создаем ViewModel с дополнительной информацией
                _allOwners = owners.Select(o => new OwnerViewModel
                {
                    owner_id = o.owner_id,
                    full_name = o.full_name,
                    passport_data = o.passport_data ?? "Не указан",
                    phone_number = o.phone_number ?? "Не указан",
                    email = o.email ?? "Не указан",
                    registration_date = o.registration_date,

                    // Подсчитываем количество квартир у собственника
                    apartments_count = _context.PropertyOwnership
                        .Count(po => po.owner_id == o.owner_id)
                }).ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка загрузки данных",
                    $"Не удалось загрузить список собственников:\n{ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filtered = _allOwners.AsEnumerable();

                // Поиск
                if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    string search = SearchTextBox.Text.ToLower();
                    filtered = filtered.Where(o =>
                        (o.full_name != null && o.full_name.ToLower().Contains(search)) ||
                        (o.passport_data != null && o.passport_data.ToLower().Contains(search)) ||
                        (o.phone_number != null && o.phone_number.Contains(search)) ||
                        (o.email != null && o.email.ToLower().Contains(search)));
                }

                // Фильтр по наличию квартир
                if (ApartmentFilterComboBox.SelectedItem != null &&
                    ApartmentFilterComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    string filter = selectedItem.Content.ToString();

                    switch (filter)
                    {
                        case "С квартирами":
                            filtered = filtered.Where(o => o.apartments_count > 0);
                            break;
                        case "Без квартир":
                            filtered = filtered.Where(o => o.apartments_count == 0);
                            break;
                            // "Все" - без фильтрации
                    }
                }

                // Сортировка по ФИО
                filtered = filtered.OrderBy(o => o.full_name);

                OwnersGrid.ItemsSource = filtered.ToList();
                OwnersCountText.Text = $" ({filtered.Count()} собственников)";
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка фильтрации",
                    $"Не удалось применить фильтры:\n{ex.Message}");
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void AddOwner_Click(object sender, RoutedEventArgs e)
        {
            // Открываем форму добавления собственника
            NavigationService.Navigate(new OwnerEditPage());
        }

        private void EditOwner_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOwner == null)
            {
                ShowWarningMessage("Выберите собственника",
                    "Пожалуйста, выберите собственника из списка для редактирования.");
                return;
            }

            // Открываем форму редактирования собственника
            NavigationService.Navigate(new OwnerEditPage(_selectedOwner.owner_id));
        }

        private void DeleteOwner_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOwner == null)
            {
                ShowWarningMessage("Выберите собственника",
                    "Пожалуйста, выберите собственника из списка для удаления.");
                return;
            }

            // Проверяем, есть ли у собственника квартиры
            if (_selectedOwner.apartments_count > 0)
            {
                ShowErrorMessage("Невозможно удалить собственника",
                    $"Собственник {_selectedOwner.full_name} владеет {_selectedOwner.apartments_count} квартирами.\n" +
                    "Перед удалением необходимо передать или освободить все квартиры.");
                return;
            }

            var result = MessageBox.Show(
                $"Удалить собственника: {_selectedOwner.full_name}?\n" +
                $"Паспорт: {_selectedOwner.passport_data}",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var ownerToDelete = _context.Owners.Find(_selectedOwner.owner_id);

                    if (ownerToDelete != null)
                    {
                        // Проверяем, нет ли других связей
                        var hasRelatedData = _context.PropertyOwnership
                            .Any(po => po.owner_id == _selectedOwner.owner_id);

                        if (hasRelatedData)
                        {
                            ShowErrorMessage("Невозможно удалить собственника",
                                "У собственника есть связанные данные. Обратитесь к администратору.");
                            return;
                        }

                        _context.Owners.Remove(ownerToDelete);
                        _context.SaveChanges();

                        ShowSuccessMessage("Собственник удален",
                            $"Собственник {_selectedOwner.full_name} успешно удален из системы.");

                        LoadData();
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Ошибка удаления",
                        $"Не удалось удалить собственника:\n{ex.Message}");
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApartmentFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void OwnersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedOwner = OwnersGrid.SelectedItem as OwnerViewModel;
        }

        // Вспомогательные методы для показа сообщений
        private void ShowErrorMessage(string title, string message)
        {
            MessageBox.Show(message, title,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowWarningMessage(string title, string message)
        {
            MessageBox.Show(message, title,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ShowSuccessMessage(string title, string message)
        {
            MessageBox.Show(message, title,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}