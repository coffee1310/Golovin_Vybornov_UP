using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

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

            // Устанавливаем дату по умолчанию
            RegistrationDatePicker.SelectedDate = DateTime.Today;

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
                        EmailTextBox.Text = _originalOwner.email ?? "";

                        if (_originalOwner.registration_date.HasValue)
                        {
                            RegistrationDatePicker.SelectedDate = _originalOwner.registration_date.Value;
                        }

                        // Показываем раздел с квартирами
                        ApartmentsLabel.Visibility = Visibility.Visible;
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
                    ApartmentsLabel.Visibility = Visibility.Collapsed;
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
                                Foreground = "#333"
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
            _originalOwner.email = EmailTextBox.Text.Trim();
            _originalOwner.registration_date = RegistrationDatePicker.SelectedDate;
        }

        private void CreateNewOwner()
        {
            var newOwner = new Owners
            {
                full_name = FullNameTextBox.Text.Trim(),
                passport_data = PassportTextBox.Text.Trim(),
                phone_number = PhoneTextBox.Text.Trim(),
                email = EmailTextBox.Text.Trim(),
                registration_date = RegistrationDatePicker.SelectedDate
            };

            _context.Owners.Add(newOwner);
        }

        private bool ValidateData()
        {
            ErrorText.Visibility = Visibility.Collapsed;
            ErrorText.Text = "";

            var errors = new List<string>();

            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
                errors.Add("• ФИО обязательно для заполнения");

            // Проверка формата телефона (если указан)
            if (!string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                // Убираем все нецифровые символы
                string phoneDigits = Regex.Replace(PhoneTextBox.Text, @"\D", "");

                if (phoneDigits.Length < 10 || phoneDigits.Length > 11)
                    errors.Add("• Телефон должен содержать 10-11 цифр");
            }

            // Проверка формата email (если указан)
            if (!string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                try
                {
                    var addr = new System.Net.Mail.MailAddress(EmailTextBox.Text);
                    if (addr.Address != EmailTextBox.Text.Trim())
                        errors.Add("• Email имеет неверный формат");
                }
                catch
                {
                    errors.Add("• Email имеет неверный формат");
                }
            }

            // Проверка уникальности паспорта (если указан)
            if (!string.IsNullOrWhiteSpace(PassportTextBox.Text))
            {
                var existingOwner = _context.Owners
                    .FirstOrDefault(o => o.passport_data == PassportTextBox.Text.Trim() &&
                                       (!_ownerId.HasValue || o.owner_id != _ownerId.Value));

                if (existingOwner != null)
                    errors.Add($"• Собственник с паспортом {PassportTextBox.Text} уже существует в системе");
            }

            if (errors.Any())
            {
                ErrorText.Text = string.Join("\n", errors);
                ErrorText.Visibility = Visibility.Visible;
                return false;
            }

            return true;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new OwnersPage());
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