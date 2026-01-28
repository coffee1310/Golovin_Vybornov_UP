using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;

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

            // Подписываемся на события ввода
            NumberTextBox.PreviewTextInput += NumberTextBox_PreviewTextInput;
            AreaTextBox.PreviewTextInput += AreaTextBox_PreviewTextInput;

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

                if (!buildingsData.Any())
                {
                    MessageBox.Show("Нет доступных зданий. Сначала добавьте здание.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NavigationService.GoBack();
                    return;
                }

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
                    new Owners { full_name = "Не выбран (опционально)", owner_id = 0 }
                };
                ownersWithEmpty.AddRange(_owners);

                OwnerComboBox.ItemsSource = ownersWithEmpty;
                OwnerComboBox.SelectedIndex = 0;

                // Загружаем примечания (опционально)
                if (_apartmentId.HasValue)
                {
                    // Можно загрузить существующие примечания если они есть
                    NoteTextBox.Text = ""; // Очищаем поле
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных формы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                NavigationService.GoBack();
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

                    if (_originalApartment == null)
                    {
                        MessageBox.Show("Квартира не найдена", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                        return;
                    }

                    PageTitle.Text = "Редактирование квартиры";

                    // Заполняем поля

                    // Здание
                    var selectedBuilding = _buildings?.FirstOrDefault(b =>
                        b.building_id == _originalApartment.building_id);

                    if (selectedBuilding != null)
                        BuildingComboBox.SelectedItem = selectedBuilding;
                    else
                        MessageBox.Show("Здание, связанное с этой квартирой, не найдено", "Внимание",
                            MessageBoxButton.OK, MessageBoxImage.Warning);

                    // Номер квартиры
                    NumberTextBox.Text = _originalApartment.apartment_number.ToString();

                    // Площадь (обязательное поле)
                    if (_originalApartment.area.HasValue)
                        AreaTextBox.Text = _originalApartment.area.Value.ToString("F2");
                    else
                        AreaTextBox.Text = ""; // Пустое поле

                    // Собственник (из PropertyOwnership, опционально)
                    var ownership = _context.PropertyOwnership
                        .FirstOrDefault(o => o.apartment_id == _originalApartment.apartment_id);

                    if (ownership != null)
                    {
                        var selectedOwner = _owners?.FirstOrDefault(o =>
                            o.owner_id == ownership.owner_id);

                        if (selectedOwner != null)
                            OwnerComboBox.SelectedItem = selectedOwner;
                        else
                            OwnerComboBox.SelectedIndex = 0;
                    }
                    else
                    {
                        // Если собственник не выбран, устанавливаем "Не выбран"
                        OwnerComboBox.SelectedIndex = 0;
                    }

                    // Примечание (опционально)
                    NoteTextBox.Text = ""; // Оставляем пустым
                }
                else
                {
                    // Режим добавления
                    PageTitle.Text = "Новая квартира";
                    OwnerComboBox.SelectedIndex = 0;
                    NoteTextBox.Text = "";
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

                // Обновляем связь с собственником (опционально)
                UpdateOwnerRelationship();

                // Сохраняем примечание (опционально)
                SaveNote();

                _context.SaveChanges();

                MessageBox.Show(
                    _apartmentId.HasValue ? "Квартира успешно обновлена!" : "Квартира успешно добавлена!",
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
            try
            {
                var selectedBuilding = BuildingComboBox.SelectedItem as BuildingViewModel;
                if (selectedBuilding != null)
                {
                    _originalApartment.building_id = selectedBuilding.building_id;
                }
                else
                {
                    throw new Exception("Не выбрано здание");
                }

                if (int.TryParse(NumberTextBox.Text, out int number))
                {
                    _originalApartment.apartment_number = number;
                }
                else
                {
                    throw new Exception("Неверный номер квартиры");
                }

                // Площадь - обязательное поле
                if (decimal.TryParse(AreaTextBox.Text, out decimal area))
                {
                    _originalApartment.area = area;
                }
                else
                {
                    throw new Exception("Неверный формат площади");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка обновления данных: {ex.Message}");
            }
        }

        private void CreateNewApartment()
        {
            try
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

                // Площадь - обязательное поле
                if (!decimal.TryParse(AreaTextBox.Text, out decimal area))
                {
                    throw new Exception("Неверный формат площади");
                }

                var newApartment = new Apartments
                {
                    building_id = selectedBuilding.building_id,
                    apartment_number = number,
                    area = area
                };

                _context.Apartments.Add(newApartment);
                _context.SaveChanges(); // Сохраняем, чтобы получить ID

                _originalApartment = newApartment;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка создания квартиры: {ex.Message}");
            }
        }

        private void UpdateOwnerRelationship()
        {
            try
            {
                var selectedOwner = OwnerComboBox.SelectedItem as Owners;

                // Если выбрано "Не выбран (опционально)" или owner_id = 0
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
            catch (Exception ex)
            {
                throw new Exception($"Ошибка обновления собственника: {ex.Message}");
            }
        }

        private void SaveNote()
        {
            try
            {
                // Примечание опционально, можно сохранять в отдельную таблицу или поле
                // Если в базе есть поле для примечаний, добавьте его сохранение здесь
                // Например:
                // if (!string.IsNullOrWhiteSpace(NoteTextBox.Text))
                // {
                //     _originalApartment.notes = NoteTextBox.Text;
                // }
                // else
                // {
                //     _originalApartment.notes = null;
                // }

                // В данном примере просто игнорируем, так как в таблице Apartments нет поля для примечаний
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка сохранения примечания: {ex.Message}");
            }
        }

        private bool ValidateData()
        {
            var errors = new List<string>();

            // Проверка здания (обязательное)
            if (BuildingComboBox.SelectedItem == null)
            {
                errors.Add("• Выберите здание");
            }

            // Проверка номера квартиры (обязательное)
            if (string.IsNullOrWhiteSpace(NumberTextBox.Text))
            {
                errors.Add("• Введите номер квартиры");
            }
            else if (!int.TryParse(NumberTextBox.Text, out int number))
            {
                errors.Add("• Номер квартиры должен быть целым числом");
            }
            else if (number <= 0)
            {
                errors.Add("• Номер квартиры должен быть положительным числом");
            }
            else if (number > 999)
            {
                errors.Add("• Номер квартиры не может превышать 999");
            }

            // Проверка площади (обязательное)
            if (string.IsNullOrWhiteSpace(AreaTextBox.Text))
            {
                errors.Add("• Введите площадь квартиры");
            }
            else if (!decimal.TryParse(AreaTextBox.Text, out decimal area))
            {
                errors.Add("• Площадь должна быть числом (разделитель - точка)");
            }
            else if (area <= 0)
            {
                errors.Add("• Площадь должна быть положительным числом");
            }
            else if (area > 10000)
            {
                errors.Add("• Площадь не может превышать 10,000 м²");
            }

            // Проверка уникальности номера квартиры в здании
            var selectedBuilding = BuildingComboBox.SelectedItem as BuildingViewModel;
            if (selectedBuilding != null && !string.IsNullOrWhiteSpace(NumberTextBox.Text) &&
                int.TryParse(NumberTextBox.Text, out int apartmentNumber) && apartmentNumber > 0)
            {
                var existingApartment = _context.Apartments
                    .FirstOrDefault(a => a.building_id == selectedBuilding.building_id &&
                                        a.apartment_number == apartmentNumber &&
                                        (!_apartmentId.HasValue || a.apartment_id != _apartmentId.Value));

                if (existingApartment != null)
                {
                    errors.Add($"• Квартира №{apartmentNumber} уже существует в этом здании");
                }
            }

            // Собственник и примечание - опциональные поля, не требуем их заполнения

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
            NavigationService.Navigate(new ApartmentsPage());
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
        private void NumberTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            NumberTextBox.SelectAll();
        }

        private void AreaTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AreaTextBox.SelectAll();
        }

        private void NoteTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            NoteTextBox.SelectAll();
        }

        private void BuildingComboBox_DropDownOpened(object sender, EventArgs e)
        {
            // Обновляем список зданий при открытии комбобокса
            try
            {
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления списка зданий: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OwnerComboBox_DropDownOpened(object sender, EventArgs e)
        {
            // Обновляем список собственников при открытии комбобокса
            try
            {
                _owners = _context.Owners
                    .OrderBy(o => o.full_name)
                    .ToList();

                var ownersWithEmpty = new List<Owners>
                {
                    new Owners { full_name = "Не выбран (опционально)", owner_id = 0 }
                };
                ownersWithEmpty.AddRange(_owners);

                OwnerComboBox.ItemsSource = ownersWithEmpty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления списка собственников: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}