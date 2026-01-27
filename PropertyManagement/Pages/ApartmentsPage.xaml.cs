using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PropertyManagement.Pages
{
    public partial class ApartmentsPage : Page
    {
        private readonly PropertyManagementEntities _context;
        private List<ApartmentViewModel> _allApartments;
        private ApartmentViewModel _selectedApartment;

        public class ApartmentViewModel
        {
            public int apartment_id { get; set; }
            public int apartment_number { get; set; }
            public decimal? area { get; set; }
            public int building_id { get; set; }
            public string building_address { get; set; }
            public string city { get; set; }
            public string owner_name { get; set; }
            public string owner_phone { get; set; }
        }

        public class BuildingFilterItem
        {
            public int building_id { get; set; }
            public string Text { get; set; }
        }

        public ApartmentsPage()
        {
            InitializeComponent();
            _context = new PropertyManagementEntities();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Получаем данные о квартирах с информацией о зданиях и собственниках
                var apartmentsData = (from apt in _context.Apartments
                                      join bld in _context.Buildings on apt.building_id equals bld.building_id
                                      join own in _context.PropertyOwnership on apt.apartment_id equals own.apartment_id into ownGroup
                                      from ownRel in ownGroup.DefaultIfEmpty()
                                      join owner in _context.Owners on (ownRel != null ? ownRel.owner_id : (int?)null) equals owner.owner_id into ownerGroup
                                      from ownerInfo in ownerGroup.DefaultIfEmpty()
                                      select new
                                      {
                                          apt.apartment_id,
                                          apt.apartment_number,
                                          apt.area,
                                          apt.building_id,
                                          building_address = bld.address,
                                          city = bld.city,
                                          owner_name = ownerInfo != null ? ownerInfo.full_name : "Не указан",
                                          owner_phone = ownerInfo != null ? ownerInfo.phone_number : ""
                                      }).ToList();

                _allApartments = apartmentsData.Select(a => new ApartmentViewModel
                {
                    apartment_id = a.apartment_id,
                    apartment_number = a.apartment_number,
                    area = a.area,
                    building_id = a.building_id,
                    building_address = a.building_address,
                    city = a.city,
                    owner_name = a.owner_name,
                    owner_phone = a.owner_phone
                }).ToList();

                ApplyFilters();
                LoadBuildingFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadBuildingFilter()
        {
            try
            {
                // Получаем уникальные здания из списка квартир
                var buildings = _allApartments
                    .Select(a => new { a.building_id, a.building_address, a.city })
                    .Distinct()
                    .OrderBy(b => b.city)
                    .ThenBy(b => b.building_address)
                    .ToList();

                var filterItems = new List<BuildingFilterItem>
                {
                    new BuildingFilterItem { building_id = 0, Text = "Все здания" }
                };

                filterItems.AddRange(buildings.Select(b => new BuildingFilterItem
                {
                    building_id = b.building_id,
                    Text = $"{b.city}, {b.building_address}"
                }));

                BuildingFilterComboBox.ItemsSource = filterItems;
                BuildingFilterComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки фильтра зданий: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            var filtered = _allApartments.AsEnumerable();

            // Фильтр по зданию
            if (BuildingFilterComboBox.SelectedItem is BuildingFilterItem selectedBuilding &&
                selectedBuilding.building_id > 0)
            {
                filtered = filtered.Where(a => a.building_id == selectedBuilding.building_id);
            }

            // Поиск
            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                string search = SearchTextBox.Text.ToLower();
                filtered = filtered.Where(a =>
                    (a.apartment_number.ToString().Contains(search)) ||
                    (a.building_address != null && a.building_address.ToLower().Contains(search)) ||
                    (a.city != null && a.city.ToLower().Contains(search)) ||
                    (a.owner_name != null && a.owner_name.ToLower().Contains(search)) ||
                    (a.owner_phone != null && a.owner_phone.Contains(search)));
            }

            // Сортировка
            filtered = filtered.OrderBy(a => a.city)
                              .ThenBy(a => a.building_address)
                              .ThenBy(a => a.apartment_number);

            ApartmentsGrid.ItemsSource = filtered.ToList();
            ApartmentsCountText.Text = $" ({filtered.Count()} квартир)";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void AddApartment_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ApartmentEditPage());
        }

        private void EditApartment_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedApartment == null)
            {
                MessageBox.Show("Выберите квартиру для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NavigationService.Navigate(new ApartmentEditPage(_selectedApartment.apartment_id));
        }

        private void DeleteApartment_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedApartment == null)
            {
                MessageBox.Show("Выберите квартиру для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить квартиру #{_selectedApartment.apartment_number}?\n" +
                $"Адрес: {_selectedApartment.building_address}",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Проверяем, есть ли связанные записи
                    var hasOwnership = _context.PropertyOwnership.Any(o => o.apartment_id == _selectedApartment.apartment_id);
                    var hasRequests = _context.ServiceRequests.Any(r => r.apartment_id == _selectedApartment.apartment_id);

                    if (hasOwnership)
                    {
                        MessageBox.Show("Невозможно удалить квартиру, так как у нее есть собственники. " +
                            "Сначала удалите связи с собственниками.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (hasRequests)
                    {
                        MessageBox.Show("Невозможно удалить квартиру, так как на нее есть заявки. " +
                            "Сначала удалите или переназначьте заявки.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var apartmentToDelete = _context.Apartments.Find(_selectedApartment.apartment_id);
                    if (apartmentToDelete != null)
                    {
                        _context.Apartments.Remove(apartmentToDelete);
                        _context.SaveChanges();

                        MessageBox.Show("Квартира удалена", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        LoadData();
                    }
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

        private void BuildingFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApartmentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedApartment = ApartmentsGrid.SelectedItem as ApartmentViewModel;
        }
    }
}