using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PropertyManagement.Pages
{
    public partial class ServiceRequestEditPage : Page
    {
        private readonly PropertyManagementEntities _context;
        private readonly int? _requestId;
        private ServiceRequests _originalRequest;

        public class ApartmentViewModel
        {
            public int apartment_id { get; set; }
            public string FullAddress { get; set; }
        }

        public class EmployeeViewModel
        {
            public int employee_id { get; set; }
            public string full_name { get; set; }
        }

        private List<ApartmentViewModel> _apartments;
        private List<EmployeeViewModel> _employees;

        public ServiceRequestEditPage(int? requestId = null)
        {
            InitializeComponent();
            _context = new PropertyManagementEntities();
            _requestId = requestId;

            LoadFormData();
            LoadRequestData();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void LoadFormData()
        {
            try
            {
                // Загружаем квартиры с адресами зданий
                var apartmentsData = (from apt in _context.Apartments
                                      join bld in _context.Buildings on apt.building_id equals bld.building_id
                                      orderby bld.address, apt.apartment_number
                                      select new
                                      {
                                          apt.apartment_id,
                                          apt.apartment_number,
                                          bld.address
                                      }).ToList();

                _apartments = apartmentsData.Select(a => new ApartmentViewModel
                {
                    apartment_id = a.apartment_id,
                    FullAddress = $"{a.address}, кв. {a.apartment_number}"
                }).ToList();

                // Добавляем опцию "Выберите квартиру"
                _apartments.Insert(0, new ApartmentViewModel
                {
                    apartment_id = 0,
                    FullAddress = "Выберите квартиру"
                });

                ApartmentComboBox.ItemsSource = _apartments;
                ApartmentComboBox.SelectedIndex = 0;

                // Загружаем сотрудников (только тех, кто может выполнять работы)
                var employeesData = _context.Employees
                    .Where(e => e.position != "Охранник" && e.position != "Уборщик") // Исключаем некоторые должности
                    .OrderBy(e => e.full_name)
                    .ToList();

                // Создаем список сотрудников
                var employeesList = new List<EmployeeViewModel>
                {
                    new EmployeeViewModel { employee_id = 0, full_name = "Не назначен" }
                };

                employeesList.AddRange(employeesData.Select(e => new EmployeeViewModel
                {
                    employee_id = e.employee_id,
                    full_name = $"{e.full_name} ({e.position})"
                }));

                _employees = employeesList;
                EmployeeComboBox.ItemsSource = _employees;
                EmployeeComboBox.SelectedIndex = 0;

                // Устанавливаем значения по умолчанию для ComboBox типов заявок
                TypeComboBox.Items.Add("Ремонт");
                TypeComboBox.Items.Add("Обслуживание");
                TypeComboBox.Items.Add("Консультация");
                TypeComboBox.Items.Add("Экстренный вызов");
                TypeComboBox.Items.Add("Установка оборудования");
                TypeComboBox.Items.Add("Проверка");
                TypeComboBox.Items.Add("Другое");
                TypeComboBox.SelectedIndex = 0;

                // Устанавливаем значения для ComboBox статусов
                StatusComboBox.Items.Add("Открыта");
                StatusComboBox.Items.Add("В работе");
                StatusComboBox.Items.Add("Выполнена");
                StatusComboBox.Items.Add("Закрыта");
                StatusComboBox.Items.Add("Отменена");
                StatusComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных формы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRequestData()
        {
            try
            {
                if (_requestId.HasValue)
                {
                    // Режим редактирования
                    _originalRequest = _context.ServiceRequests
                        .FirstOrDefault(r => r.request_id == _requestId.Value);

                    if (_originalRequest != null)
                    {
                        PageTitle.Text = "Редактирование заявки";

                        // Заполняем поля

                        // Квартира
                        if (_originalRequest.apartment_id > 0)
                        {
                            var selectedApartment = _apartments.FirstOrDefault(a =>
                                a.apartment_id == _originalRequest.apartment_id);

                            if (selectedApartment != null)
                                ApartmentComboBox.SelectedItem = selectedApartment;
                        }

                        // Тип заявки
                        if (!string.IsNullOrEmpty(_originalRequest.request_type))
                        {
                            TypeComboBox.SelectedItem = _originalRequest.request_type;
                        }

                        // Описание
                        DescriptionTextBox.Text = _originalRequest.description ?? "";

                        // Статус
                        if (!string.IsNullOrEmpty(_originalRequest.status))
                        {
                            StatusComboBox.SelectedItem = _originalRequest.status;
                        }

                        // Исполнитель
                        if (_originalRequest.employee_id.HasValue && _originalRequest.employee_id.Value > 0)
                        {
                            var selectedEmployee = _employees.FirstOrDefault(e =>
                                e.employee_id == _originalRequest.employee_id.Value);

                            if (selectedEmployee != null)
                                EmployeeComboBox.SelectedItem = selectedEmployee;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Заявка не найдена", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                    }
                }
                else
                {
                    // Режим добавления
                    PageTitle.Text = "Новая заявка";
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
                if (_requestId.HasValue)
                {
                    // Обновляем существующую заявку
                    UpdateExistingRequest();
                }
                else
                {
                    // Создаем новую заявку
                    CreateNewRequest();
                }

                _context.SaveChanges();

                MessageBox.Show(
                    _requestId.HasValue ? "Заявка обновлена!" : "Заявка создана!",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Возвращаемся к списку заявок
                NavigationService.Navigate(new ServiceRequestsPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateExistingRequest()
        {
            var selectedApartment = ApartmentComboBox.SelectedItem as ApartmentViewModel;
            if (selectedApartment != null && selectedApartment.apartment_id > 0)
            {
                _originalRequest.apartment_id = selectedApartment.apartment_id;
            }

            _originalRequest.request_type = TypeComboBox.SelectedItem?.ToString() ?? "";
            _originalRequest.description = DescriptionTextBox.Text.Trim();
            _originalRequest.status = StatusComboBox.SelectedItem?.ToString() ?? "Открыта";

            var selectedEmployee = EmployeeComboBox.SelectedItem as EmployeeViewModel;
            if (selectedEmployee != null && selectedEmployee.employee_id > 0)
            {
                _originalRequest.employee_id = selectedEmployee.employee_id;
            }
            else
            {
                _originalRequest.employee_id = null;
            }

            // Обновляем дату изменения

            // Если статус изменился на "Выполнена" или "Закрыта", устанавливаем дату завершения
            if ((_originalRequest.status == "Выполнена" || _originalRequest.status == "Закрыта")
                && !_originalRequest.completed_date.HasValue)
            {
                _originalRequest.completed_date = DateTime.Now;
            }
        }

        private void CreateNewRequest()
        {
            var selectedApartment = ApartmentComboBox.SelectedItem as ApartmentViewModel;
            if (selectedApartment == null || selectedApartment.apartment_id == 0)
            {
                throw new Exception("Не выбрана квартира");
            }

            var newRequest = new ServiceRequests
            {
                apartment_id = selectedApartment.apartment_id,
                request_type = TypeComboBox.SelectedItem?.ToString() ?? "",
                description = DescriptionTextBox.Text.Trim(),
                status = StatusComboBox.SelectedItem?.ToString() ?? "Открыта",
                created_date = DateTime.Now,
            };

            // Устанавливаем дату завершения если статус сразу "Выполнена" или "Закрыта"
            if (newRequest.status == "Выполнена" || newRequest.status == "Закрыта")
            {
                newRequest.completed_date = DateTime.Now;
            }

            var selectedEmployee = EmployeeComboBox.SelectedItem as EmployeeViewModel;
            if (selectedEmployee != null && selectedEmployee.employee_id > 0)
            {
                newRequest.employee_id = selectedEmployee.employee_id;
            }

            _context.ServiceRequests.Add(newRequest);
        }

        private bool ValidateData()
        {
            var errors = new List<string>();

            // Проверка квартиры
            var selectedApartment = ApartmentComboBox.SelectedItem as ApartmentViewModel;
            if (selectedApartment == null || selectedApartment.apartment_id == 0)
                errors.Add("• Выберите квартиру");

            // Проверка типа заявки
            if (TypeComboBox.SelectedItem == null)
                errors.Add("• Выберите тип заявки");

            // Проверка описания
            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
                errors.Add("• Введите описание заявки");
            else if (DescriptionTextBox.Text.Trim().Length < 10)
                errors.Add("• Описание должно содержать минимум 10 символов");
            else if (DescriptionTextBox.Text.Trim().Length > 1000)
                errors.Add("• Описание не может превышать 1000 символов");

            // Проверка статуса
            if (StatusComboBox.SelectedItem == null)
                errors.Add("• Выберите статус заявки");

            // Проверка логики статусов
            if (StatusComboBox.SelectedItem?.ToString() == "Выполнена" ||
                StatusComboBox.SelectedItem?.ToString() == "Закрыта")
            {
                var selectedEmployee = EmployeeComboBox.SelectedItem as EmployeeViewModel;
                if (selectedEmployee == null || selectedEmployee.employee_id == 0)
                {
                    errors.Add("• Для закрытия или завершения заявки необходимо назначить исполнителя");
                }
            }

            if (errors.Count > 0)
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
            NavigationService.Navigate(new ServiceRequestsPage());
        }

        // Обработчики для проверки ввода

        // Ограничение длины описания
        private void DescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DescriptionTextBox.Text.Length > 1000)
            {
                DescriptionTextBox.Text = DescriptionTextBox.Text.Substring(0, 1000);
                DescriptionTextBox.CaretIndex = 1000;

                MessageBox.Show("Описание не может превышать 1000 символов",
                    "Предупреждение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        // Автоматический выбор исполнителя при смене типа заявки
        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно добавить логику автоматического назначения исполнителя
            // в зависимости от типа заявки
            if (TypeComboBox.SelectedItem != null && !_requestId.HasValue)
            {
                string selectedType = TypeComboBox.SelectedItem.ToString();

                // Пример логики: для ремонтов назначаем техников
                if (selectedType == "Ремонт" && EmployeeComboBox.SelectedIndex == 0)
                {
                    // Ищем техника в списке сотрудников
                    var technician = _employees.FirstOrDefault(emp =>
                        emp.full_name.Contains("Техник") ||
                        emp.full_name.Contains("Электрик") ||
                        emp.full_name.Contains("Сантехник"));

                    if (technician != null)
                    {
                        EmployeeComboBox.SelectedItem = technician;
                    }
                }
            }
        }

        // Обработчик изменения статуса
        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatusComboBox.SelectedItem != null)
            {
                string selectedStatus = StatusComboBox.SelectedItem.ToString();

                // Предупреждение при попытке закрыть заявку без исполнителя
                if ((selectedStatus == "Выполнена" || selectedStatus == "Закрыта") &&
                    EmployeeComboBox.SelectedIndex == 0)
                {
                    MessageBox.Show("Рекомендуется назначить исполнителя перед закрытием заявки",
                        "Предупреждение",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }
    }
}