using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PropertyManagement.Pages
{
    public partial class OwnerEditPage : Page
    {
        private readonly PropertyManagementEntities _context;
        private readonly int? _ownerId;
        private Owners _originalOwner;
        private List<ApartmentInfo> _ownerApartments;

        public class ApartmentInfo
        {
            public string DisplayText { get; set; }
            public int ApartmentId { get; set; }
        }

        public OwnerEditPage(int? ownerId = null)
        {
            InitializeComponent();
            _context = new PropertyManagementEntities();
            _ownerId = ownerId;

            LoadOwnerData();
            LoadOwnerApartments();
        }

        private void LoadOwnerData()
        {
            try
            {
                if (_ownerId.HasValue)
                {
                    // Режим редактирования
                    _originalOwner = _context.Owners.Find(_ownerId.Value);

                    if (_originalOwner != null)
                    {
                        PageTitle.Text = "Редактирование собственника";

                        // Заполняем поля
                        FullNameTextBox.Text = _originalOwner.full_name ?? "";
                        PassportTextBox.Text = _originalOwner.passport_data ?? "";
                        PhoneTextBox.Text = _originalOwner.phone_number ?? "";

                        // Показываем раздел с квартирами
                        ApartmentsPanel.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        ShowErrorMessage("Собственник не найден",
                            "Выбранный собственник не существует в базе данных.");
                        NavigationService.GoBack();
                    }
                }
                else
                {
                    // Режим добавления
                    PageTitle.Text = "Новый собственник";

                    // Скрываем раздел с квартирами
                    ApartmentsPanel.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка загрузки",
                    $"Не удалось загрузить данные собственника:\n{ex.Message}");
                NavigationService.GoBack();
            }
        }

        private void LoadOwnerApartments()
        {
            try
            {
                if (_ownerId.HasValue)
                {
                    // Получаем квартиры собственника
                    var apartments = (from po in _context.PropertyOwnership
                                      join apt in _context.Apartments on po.apartment_id equals apt.apartment_id
                                      join bld in _context.Buildings on apt.building_id equals bld.building_id
                                      where po.owner_id == _ownerId.Value
                                      orderby bld.city, bld.address, apt.apartment_number
                                      select new ApartmentInfo
                                      {
                                          DisplayText = $"{bld.city}, {bld.address}, кв. {apt.apartment_number}",
                                          ApartmentId = apt.apartment_id
                                      }).ToList();

                    _ownerApartments = apartments;

                    if (apartments.Any())
                    {
                        // Создаем список для отображения
                        var stackPanel = new StackPanel();

                        foreach (var apt in apartments)
                        {
                            var textBlock = new TextBlock
                            {
                                Text = $"• {apt.DisplayText}",
                                Margin = new Thickness(0, 2, 0, 2),
                                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333"))
                            };
                            stackPanel.Children.Add(textBlock);
                        }

                        ApartmentsListBox.ItemsSource = new List<UIElement> { stackPanel };
                        NoApartmentsText.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        NoApartmentsText.Visibility = Visibility.Visible;
                        NoApartmentsText.Text = "У собственника нет квартир";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки квартир: {ex.Message}");
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
                if (_ownerId.HasValue)
                {
                    // Обновляем существующего собственника
                    UpdateExistingOwner();
                }
                else
                {
                    // Создаем нового собственника
                    CreateNewOwner();
                }

                _context.SaveChanges();

                ShowSuccessMessage("Сохранение успешно",
                    _ownerId.HasValue ? "Данные собственника обновлены!" : "Новый собственник добавлен!");

                // Возвращаемся к списку собственников
                NavigationService.Navigate(new OwnersPage());
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка сохранения",
                    $"Не удалось сохранить данные:\n{ex.Message}");
            }
        }

        private void UpdateExistingOwner()
        {
            if (_originalOwner == null) return;

            _originalOwner.full_name = FullNameTextBox.Text.Trim();
            _originalOwner.passport_data = PassportTextBox.Text.Trim();
            _originalOwner.phone_number = PhoneTextBox.Text.Trim();
        }

        private void CreateNewOwner()
        {
            var newOwner = new Owners
            {
                full_name = FullNameTextBox.Text.Trim(),
                passport_data = PassportTextBox.Text.Trim(),
                phone_number = PhoneTextBox.Text.Trim()
            };

            _context.Owners.Add(newOwner);
        }

        private bool ValidateData()
        {
            var errors = new List<string>();

            // Проверка ФИО (обязательное)
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
                errors.Add("• ФИО обязательно для заполнения");
            else if (FullNameTextBox.Text.Trim().Length < 5)
                errors.Add("• ФИО должно содержать минимум 5 символов");
            else if (FullNameTextBox.Text.Trim().Length > 255)
                errors.Add("• ФИО не может превышать 255 символов");

            // Проверка паспортных данных (обязательное)
            if (string.IsNullOrWhiteSpace(PassportTextBox.Text))
                errors.Add("• Паспортные данные обязательны для заполнения");
            else if (PassportTextBox.Text.Trim().Length < 5)
                errors.Add("• Паспортные данные должны содержать минимум 5 символов");
            else if (PassportTextBox.Text.Trim().Length > 100)
                errors.Add("• Паспортные данные не могут превышать 100 символов");
            else
            {
                // Проверка уникальности паспорта
                var existingOwner = _context.Owners
                    .FirstOrDefault(o => o.passport_data == PassportTextBox.Text.Trim() &&
                                       (!_ownerId.HasValue || o.owner_id != _ownerId.Value));

                if (existingOwner != null)
                    errors.Add($"• Собственник с паспортом '{PassportTextBox.Text.Trim()}' уже существует в системе");
            }

            // Проверка телефона (обязательное)
            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
                errors.Add("• Телефон обязателен для заполнения");
            else
            {
                // Убираем все нецифровые символы
                string phoneDigits = Regex.Replace(PhoneTextBox.Text, @"\D", "");

                if (phoneDigits.Length != 11 && phoneDigits.Length != 10)
                    errors.Add("• Телефон должен содержать 10 или 11 цифр");
                else if (!IsValidPhoneNumber(PhoneTextBox.Text))
                    errors.Add("• Введите корректный номер телефона (например: +7(XXX)XXX-XX-XX или 8XXXXXXXXXX)");

                // Проверка уникальности телефона
                var existingOwnerByPhone = _context.Owners
                    .FirstOrDefault(o => o.phone_number == PhoneTextBox.Text.Trim() &&
                                       (!_ownerId.HasValue || o.owner_id != _ownerId.Value));

                if (existingOwnerByPhone != null)
                    errors.Add($"• Собственник с телефоном '{PhoneTextBox.Text.Trim()}' уже существует в системе");
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

        private bool IsValidPhoneNumber(string phone)
        {
            // Проверяем несколько форматов телефонов
            var patterns = new[]
            {
                @"^\+7\(\d{3}\)\d{3}-\d{2}-\d{2}$", // +7(XXX)XXX-XX-XX
                @"^8\d{10}$", // 8XXXXXXXXXX
                @"^\+7\d{10}$", // +7XXXXXXXXXX
                @"^\d{10}$", // XXXXXXXXXX
                @"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$" // +7 (XXX) XXX-XX-XX
            };

            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(phone, pattern))
                    return true;
            }

            return false;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new OwnersPage());
        }

        // Дополнительные методы для улучшения UX
        private void FullNameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            FullNameTextBox.SelectAll();
        }

        private void PassportTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            PassportTextBox.SelectAll();
        }

        private void PhoneTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            PhoneTextBox.SelectAll();
        }

        // Маска для телефона
        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Автоматическое форматирование телефона
            if (PhoneTextBox.IsFocused)
            {
                string text = PhoneTextBox.Text;
                string digitsOnly = Regex.Replace(text, @"\D", "");

                if (digitsOnly.Length <= 1)
                {
                    if (!text.StartsWith("+") && !text.StartsWith("8"))
                    {
                        PhoneTextBox.Text = "8";
                        PhoneTextBox.CaretIndex = 1;
                    }
                }
                else if (digitsOnly.Length == 11)
                {
                    if (digitsOnly.StartsWith("7") || digitsOnly.StartsWith("8"))
                    {
                        string formatted = $"+7 ({digitsOnly.Substring(1, 3)}) {digitsOnly.Substring(4, 3)}-{digitsOnly.Substring(7, 2)}-{digitsOnly.Substring(9, 2)}";
                        PhoneTextBox.Text = formatted;
                    }
                }
            }
        }

        // Вспомогательные методы для показа сообщений
        private void ShowErrorMessage(string title, string message)
        {
            MessageBox.Show(message, title,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowSuccessMessage(string title, string message)
        {
            MessageBox.Show(message, title,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}