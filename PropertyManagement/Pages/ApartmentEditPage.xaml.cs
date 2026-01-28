using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PropertyManagement.Pages
{
    public partial class ApartmentEditPage : Page
    {
        private readonly PropertyManagementEntities _context;
        private readonly int? _apartmentId;
        private Apartments _originalApartment;

        public class BuildingViewModel
        {
            public int building_id { get; set; }
            public string Text { get; set; }
        }

        private List<BuildingViewModel> _buildings;
        private List<Owners> _owners;

        public ApartmentEditPage(int? apartmentId = null)
        {
            InitializeComponent();
            _context = new PropertyManagementEntities();
            _apartmentId = apartmentId;

            LoadFormData();
            LoadApartmentData();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
        private void LoadFormData()
        {
            try
            {
                // Загружаем здания
                var buildingsData = _context.Buildings
                    .OrderBy(b => b.city)
                    .ThenBy(b => b.address)
                    .ToList();

                _buildings = buildingsData.Select(b => new BuildingViewModel
                {
                    building_id = b.building_id,
                    Text = $"{b.city}, {b.address}"
                }).ToList();

                BuildingComboBox.ItemsSource = _buildings;
                if (_buildings.Any())
                    BuildingComboBox.SelectedIndex = 0;

                // Загружаем собственников
                _owners = _context.Owners
                    .OrderBy(o => o.full_name)
                    .ToList();

                // Создаем новый список с элементом "Не выбран"
                var ownersWithEmpty = new List<Owners>
        {
            new Owners { full_name = "Не выбран", owner_id = 0 }
        };
                ownersWithEmpty.AddRange(_owners);

                OwnerComboBox.ItemsSource = ownersWithEmpty;
                OwnerComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных формы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadApartmentData()
        {
            try
            {
                if (_apartmentId.HasValue)
                {
                    // Режим редактирования
                    _originalApartment = _context.Apartments
                        .FirstOrDefault(a => a.apartment_id == _apartmentId.Value);

                    if (_originalApartment != null)
                    {
                        PageTitle.Text = "Редактирование квартиры";

                        // Заполняем поля

                        // Здание
                        var selectedBuilding = _buildings.FirstOrDefault(b =>
                            b.building_id == _originalApartment.building_id);

                        if (selectedBuilding != null)
                            BuildingComboBox.SelectedItem = selectedBuilding;

                        // Номер квартиры
                        NumberTextBox.Text = _originalApartment.apartment_number.ToString();

                        // Площадь
                        if (_originalApartment.area.HasValue)
                            AreaTextBox.Text = _originalApartment.area.Value.ToString();

                        // Собственник (из PropertyOwnership)
                        var ownership = _context.PropertyOwnership
                            .FirstOrDefault(o => o.apartment_id == _originalApartment.apartment_id);

                        if (ownership != null)
                        {
                            var selectedOwner = _owners.FirstOrDefault(o =>
                                o.owner_id == ownership.owner_id);

                            if (selectedOwner != null)
                                OwnerComboBox.SelectedItem = selectedOwner;
                        }
                        else
                        {
                            // Если собственник не выбран, устанавливаем "Не выбран"
                            OwnerComboBox.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Квартира не найдена", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                    }
                }
                else
                {
                    // Режим добавления
                    PageTitle.Text = "Новая квартира";
                    OwnerComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                NavigationService.GoBack();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateData())
            {
                return;
            }

            try
            {
                if (_apartmentId.HasValue)
                {
                    // Обновляем существующую квартиру
                    UpdateExistingApartment();
                }
                else
                {
                    // Создаем новую квартиру
                    CreateNewApartment();
                }

                _context.SaveChanges();

                // Обновляем связь с собственником
                UpdateOwnerRelationship();

                _context.SaveChanges();

                MessageBox.Show(
                    _apartmentId.HasValue ? "Квартира обновлена!" : "Квартира добавлена!",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Возвращаемся к списку квартир
                NavigationService.Navigate(new ApartmentsPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateExistingApartment()
        {
            var selectedBuilding = BuildingComboBox.SelectedItem as BuildingViewModel;
            if (selectedBuilding != null)
            {
                _originalApartment.building_id = selectedBuilding.building_id;
            }

            if (int.TryParse(NumberTextBox.Text, out int number))
            {
                _originalApartment.apartment_number = number;
            }

            if (decimal.TryParse(AreaTextBox.Text, out decimal area))
            {
                _originalApartment.area = area;
            }
            else
            {
                _originalApartment.area = null;
            }
        }

        private void CreateNewApartment()
        {
            var selectedBuilding = BuildingComboBox.SelectedItem as BuildingViewModel;
            if (selectedBuilding == null)
            {
                throw new Exception("Не выбрано здание");
            }

            if (!int.TryParse(NumberTextBox.Text, out int number))
            {
                throw new Exception("Неверный номер квартиры");
            }

            var newApartment = new Apartments
            {
                building_id = selectedBuilding.building_id,
                apartment_number = number
            };

            if (decimal.TryParse(AreaTextBox.Text, out decimal area))
            {
                newApartment.area = area;
            }

            _context.Apartments.Add(newApartment);
            _context.SaveChanges(); // Сохраняем, чтобы получить ID

            _originalApartment = newApartment;
        }

        private void UpdateOwnerRelationship()
        {
            var selectedOwner = OwnerComboBox.SelectedItem as Owners;
            if (selectedOwner == null || selectedOwner.owner_id == 0)
            {
                // Удаляем существующую связь, если есть
                var existingOwnership = _context.PropertyOwnership
                    .FirstOrDefault(o => o.apartment_id == _originalApartment.apartment_id);

                if (existingOwnership != null)
                {
                    _context.PropertyOwnership.Remove(existingOwnership);
                }
                return;
            }

            // Проверяем, есть ли уже связь
            var ownership = _context.PropertyOwnership
                .FirstOrDefault(o => o.apartment_id == _originalApartment.apartment_id);

            if (ownership != null)
            {
                // Обновляем существующую связь
                ownership.owner_id = selectedOwner.owner_id;
            }
            else
            {
                // Создаем новую связь
                var newOwnership = new PropertyOwnership
                {
                    apartment_id = _originalApartment.apartment_id,
                    owner_id = selectedOwner.owner_id
                };
                _context.PropertyOwnership.Add(newOwnership);
            }
        }

        private bool ValidateData()
        {
            ErrorText.Visibility = Visibility.Collapsed;
            ErrorText.Text = "";

            var errors = "";

            // Проверка здания
            if (BuildingComboBox.SelectedItem == null)
                errors += "• Выберите здание\n";

            // Проверка номера квартиры
            if (string.IsNullOrWhiteSpace(NumberTextBox.Text))
                errors += "• Введите номер квартиры\n";
            else if (!int.TryParse(NumberTextBox.Text, out int number) || number <= 0)
                errors += "• Номер квартиры должен быть положительным числом\n";

            // Проверка площади
            if (!string.IsNullOrWhiteSpace(AreaTextBox.Text) && !decimal.TryParse(AreaTextBox.Text, out _))
                errors += "• Площадь должна быть числом\n";

            // Проверка уникальности номера квартиры в здании
            var selectedBuilding = BuildingComboBox.SelectedItem as BuildingViewModel;
            if (selectedBuilding != null && !string.IsNullOrWhiteSpace(NumberTextBox.Text) &&
                int.TryParse(NumberTextBox.Text, out int apartmentNumber))
            {
                var existingApartment = _context.Apartments
                    .FirstOrDefault(a => a.building_id == selectedBuilding.building_id &&
                                        a.apartment_number == apartmentNumber &&
                                        (!_apartmentId.HasValue || a.apartment_id != _apartmentId.Value));

                if (existingApartment != null)
                    errors += $"• Квартира №{apartmentNumber} уже существует в этом здании\n";
            }

            if (!string.IsNullOrEmpty(errors))
            {
                ErrorText.Text = errors;
                ErrorText.Visibility = Visibility.Visible;
                return false;
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ApartmentsPage());
        }
    }
}