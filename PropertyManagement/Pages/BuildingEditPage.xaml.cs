using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PropertyManagement.Pages
{
    public partial class BuildingEditPage : Page
    {
        private readonly PropertyManagementEntities _context;
        private readonly int? _buildingId;
        private Buildings _originalBuilding;

        public BuildingEditPage(int? buildingId = null)
        {
            InitializeComponent();
            _context = new PropertyManagementEntities();
            _buildingId = buildingId;

            LoadBuildingData();
        }

        private void LoadBuildingData()
        {
            try
            {
                if (_buildingId.HasValue)
                {
                    // Режим редактирования
                    _originalBuilding = _context.Buildings.Find(_buildingId.Value);

                    if (_originalBuilding != null)
                    {
                        PageTitle.Text = "Редактирование здания";

                        // Заполняем поля
                        AddressTextBox.Text = _originalBuilding.address ?? "";
                        CityTextBox.Text = _originalBuilding.city ?? "";
                        StartDatePicker.SelectedDate = _originalBuilding.management_start_date;
                        FloorsTextBox.Text = _originalBuilding.floors?.ToString() ?? "";
                        ApartmentsTextBox.Text = _originalBuilding.apartments_count?.ToString() ?? "";
                        YearTextBox.Text = _originalBuilding.construction_year?.ToString() ?? "";
                        AreaTextBox.Text = _originalBuilding.total_area?.ToString() ?? "";
                    }
                    else
                    {
                        MessageBox.Show("Здание не найдено", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                    }
                }
                else
                {
                    // Режим добавления
                    PageTitle.Text = "Добавление нового здания";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                NavigationService.GoBack();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateData())
            {
                return;
            }

            try
            {
                if (_buildingId.HasValue)
                {
                    // Обновляем существующее здание
                    UpdateExistingBuilding();
                }
                else
                {
                    // Создаем новое здание
                    CreateNewBuilding();
                }

                _context.SaveChanges();

                MessageBox.Show(
                    _buildingId.HasValue ? "Здание обновлено!" : "Здание добавлено!",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Возвращаемся к списку зданий
                NavigationService.Navigate(new BuildingsPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateExistingBuilding()
        {
            _originalBuilding.address = AddressTextBox.Text.Trim();
            _originalBuilding.city = CityTextBox.Text.Trim();
            _originalBuilding.management_start_date = StartDatePicker.SelectedDate;

            if (int.TryParse(FloorsTextBox.Text, out int floors))
                _originalBuilding.floors = floors;
            else
                _originalBuilding.floors = null;

            if (int.TryParse(ApartmentsTextBox.Text, out int apartments))
                _originalBuilding.apartments_count = apartments;
            else
                _originalBuilding.apartments_count = null;

            if (int.TryParse(YearTextBox.Text, out int year))
                _originalBuilding.construction_year = year;
            else
                _originalBuilding.construction_year = null;

            if (decimal.TryParse(AreaTextBox.Text, out decimal area))
                _originalBuilding.total_area = area;
            else
                _originalBuilding.total_area = null;
        }

        private void CreateNewBuilding()
        {
            var newBuilding = new Buildings
            {
                address = AddressTextBox.Text.Trim(),
                city = CityTextBox.Text.Trim(),
                management_start_date = StartDatePicker.SelectedDate
            };

            if (int.TryParse(FloorsTextBox.Text, out int floors))
                newBuilding.floors = floors;

            if (int.TryParse(ApartmentsTextBox.Text, out int apartments))
                newBuilding.apartments_count = apartments;

            if (int.TryParse(YearTextBox.Text, out int year))
                newBuilding.construction_year = year;

            if (decimal.TryParse(AreaTextBox.Text, out decimal area))
                newBuilding.total_area = area;

            _context.Buildings.Add(newBuilding);
        }

        private bool ValidateData()
        {
            ErrorText.Visibility = Visibility.Collapsed;
            ErrorText.Text = "";

            var errors = "";

            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(AddressTextBox.Text))
                errors += "• Адрес обязателен\n";

            if (string.IsNullOrWhiteSpace(CityTextBox.Text))
                errors += "• Город обязателен\n";

            // Проверка числовых полей
            if (!string.IsNullOrWhiteSpace(FloorsTextBox.Text) && !int.TryParse(FloorsTextBox.Text, out _))
                errors += "• Этажи: введите число\n";

            if (!string.IsNullOrWhiteSpace(ApartmentsTextBox.Text) && !int.TryParse(ApartmentsTextBox.Text, out _))
                errors += "• Квартиры: введите число\n";

            if (!string.IsNullOrWhiteSpace(YearTextBox.Text) && !int.TryParse(YearTextBox.Text, out _))
                errors += "• Год постройки: введите число\n";

            if (!string.IsNullOrWhiteSpace(AreaTextBox.Text) && !decimal.TryParse(AreaTextBox.Text, out _))
                errors += "• Площадь: введите число\n";

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
            // Возвращаемся к списку зданий
            NavigationService.Navigate(new BuildingsPage());
        }
    }
}