using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;

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

            // Подписываемся на события ввода
            FloorsTextBox.PreviewTextInput += NumberTextBox_PreviewTextInput;
            ApartmentsTextBox.PreviewTextInput += NumberTextBox_PreviewTextInput;
            YearTextBox.PreviewTextInput += NumberTextBox_PreviewTextInput;
            AreaTextBox.PreviewTextInput += AreaTextBox_PreviewTextInput;

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
                        AreaTextBox.Text = _originalBuilding.total_area?.ToString("F2") ?? "";
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
                    _buildingId.HasValue ? "Здание успешно обновлено!" : "Здание успешно добавлено!",
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
            try
            {
                _originalBuilding.address = AddressTextBox.Text.Trim();
                _originalBuilding.city = CityTextBox.Text.Trim();
                _originalBuilding.management_start_date = StartDatePicker.SelectedDate;

                // Все поля обязательны
                if (int.TryParse(FloorsTextBox.Text, out int floors) && floors > 0)
                    _originalBuilding.floors = floors;
                else
                    throw new Exception("Неверное количество этажей");

                if (int.TryParse(ApartmentsTextBox.Text, out int apartments) && apartments > 0)
                    _originalBuilding.apartments_count = apartments;
                else
                    throw new Exception("Неверное количество квартир");

                if (int.TryParse(YearTextBox.Text, out int year))
                {
                    if (year < 1800 || year > DateTime.Now.Year + 1)
                        throw new Exception($"Год постройки должен быть между 1800 и {DateTime.Now.Year + 1}");
                    _originalBuilding.construction_year = year;
                }
                else
                    throw new Exception("Неверный год постройки");

                if (decimal.TryParse(AreaTextBox.Text, out decimal area) && area > 0)
                    _originalBuilding.total_area = area;
                else
                    throw new Exception("Неверная общая площадь");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка обновления данных: {ex.Message}");
            }
        }

        private void CreateNewBuilding()
        {
            try
            {
                var newBuilding = new Buildings
                {
                    address = AddressTextBox.Text.Trim(),
                    city = CityTextBox.Text.Trim(),
                    management_start_date = StartDatePicker.SelectedDate
                };

                // Все поля обязательны
                if (int.TryParse(FloorsTextBox.Text, out int floors) && floors > 0)
                    newBuilding.floors = floors;
                else
                    throw new Exception("Неверное количество этажей");

                if (int.TryParse(ApartmentsTextBox.Text, out int apartments) && apartments > 0)
                    newBuilding.apartments_count = apartments;
                else
                    throw new Exception("Неверное количество квартир");

                if (int.TryParse(YearTextBox.Text, out int year))
                {
                    if (year < 1800 || year > DateTime.Now.Year + 1)
                        throw new Exception($"Год постройки должен быть между 1800 и {DateTime.Now.Year + 1}");
                    newBuilding.construction_year = year;
                }
                else
                    throw new Exception("Неверный год постройки");

                if (decimal.TryParse(AreaTextBox.Text, out decimal area) && area > 0)
                    newBuilding.total_area = area;
                else
                    throw new Exception("Неверная общая площадь");

                _context.Buildings.Add(newBuilding);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка создания здания: {ex.Message}");
            }
        }

        private bool ValidateData()
        {
            var errors = new System.Collections.Generic.List<string>();

            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(AddressTextBox.Text))
                errors.Add("• Введите адрес здания");

            if (string.IsNullOrWhiteSpace(CityTextBox.Text))
                errors.Add("• Введите город");

            if (!StartDatePicker.SelectedDate.HasValue)
                errors.Add("• Выберите дату начала управления");
            else if (StartDatePicker.SelectedDate.Value > DateTime.Now)
                errors.Add("• Дата начала управления не может быть в будущем");

            // Проверка этажей
            if (string.IsNullOrWhiteSpace(FloorsTextBox.Text))
                errors.Add("• Введите количество этажей");
            else if (!int.TryParse(FloorsTextBox.Text, out int floors))
                errors.Add("• Количество этажей должно быть целым числом");
            else if (floors <= 0)
                errors.Add("• Количество этажей должно быть положительным числом");
            else if (floors > 100)
                errors.Add("• Количество этажей не может превышать 100");

            // Проверка квартир
            if (string.IsNullOrWhiteSpace(ApartmentsTextBox.Text))
                errors.Add("• Введите количество квартир");
            else if (!int.TryParse(ApartmentsTextBox.Text, out int apartments))
                errors.Add("• Количество квартир должно быть целым числом");
            else if (apartments <= 0)
                errors.Add("• Количество квартир должно быть положительным числом");
            else if (apartments > 1000)
                errors.Add("• Количество квартир не может превышать 1000");

            // Проверка года постройки
            if (string.IsNullOrWhiteSpace(YearTextBox.Text))
                errors.Add("• Введите год постройки");
            else if (!int.TryParse(YearTextBox.Text, out int year))
                errors.Add("• Год постройки должен быть целым числом");
            else if (year < 1800)
                errors.Add("• Год постройки не может быть раньше 1800");
            else if (year > DateTime.Now.Year + 1)
                errors.Add($"• Год постройки не может быть позже {DateTime.Now.Year + 1}");

            // Проверка площади
            if (string.IsNullOrWhiteSpace(AreaTextBox.Text))
                errors.Add("• Введите общую площадь");
            else if (!decimal.TryParse(AreaTextBox.Text, out decimal area))
                errors.Add("• Площадь должна быть числом (разделитель - точка)");
            else if (area <= 0)
                errors.Add("• Площадь должна быть положительным числом");
            else if (area > 100000)
                errors.Add("• Площадь не может превышать 100,000 м²");

            // Проверка уникальности адреса в городе
            if (!string.IsNullOrWhiteSpace(AddressTextBox.Text) && !string.IsNullOrWhiteSpace(CityTextBox.Text))
            {
                var existingBuilding = _context.Buildings
                    .FirstOrDefault(b => b.address.ToLower().Trim() == AddressTextBox.Text.ToLower().Trim() &&
                                        b.city.ToLower().Trim() == CityTextBox.Text.ToLower().Trim() &&
                                        (!_buildingId.HasValue || b.building_id != _buildingId.Value));

                if (existingBuilding != null)
                {
                    errors.Add($"• Здание по адресу '{AddressTextBox.Text.Trim()}' уже существует в городе '{CityTextBox.Text.Trim()}'");
                }
            }

            // Отображение ошибок
            if (errors.Any())
            {
                var errorMessage = "Пожалуйста, исправьте следующие ошибки:\n\n" +
                                  string.Join("\n", errors);

                MessageBox.Show(errorMessage, "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                return false;
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new BuildingsPage());
        }

        // Методы для проверки ввода в реальном времени
        private void NumberTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void AreaTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Разрешаем цифры и точку (только одну точку)
            string currentText = AreaTextBox.Text;

            // Проверяем, есть ли уже точка в тексте
            if (e.Text == ".")
            {
                if (currentText.Contains("."))
                {
                    e.Handled = true; // Не разрешаем вторую точку
                    return;
                }
            }

            // Разрешаем только цифры и точку
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private bool IsTextAllowed(string text)
        {
            // Регулярное выражение, которое разрешает только цифры
            Regex regex = new Regex("[^0-9]+");
            return !regex.IsMatch(text);
        }

        // Дополнительные методы для улучшения UX
        private void AddressTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AddressTextBox.SelectAll();
        }

        private void CityTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            CityTextBox.SelectAll();
        }

        private void FloorsTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            FloorsTextBox.SelectAll();
        }

        private void ApartmentsTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ApartmentsTextBox.SelectAll();
        }

        private void YearTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            YearTextBox.SelectAll();
        }

        private void AreaTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AreaTextBox.SelectAll();
        }
    }
}