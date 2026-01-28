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

                // Загружаем сотрудников
                var employeesData = _context.Employees.ToList();

                // Создаем список сотрудников
                var employeesList = new List<EmployeeViewModel>
                {
                    new EmployeeViewModel { employee_id = 0, full_name = "Не назначен" }
                };

                employeesList.AddRange(employeesData.Select(e => new EmployeeViewModel
                {
                    employee_id = e.employee_id,
                    full_name = e.full_name
                }));

                _employees = employeesList;
                EmployeeComboBox.ItemsSource = _employees;
                EmployeeComboBox.SelectedIndex = 0;

                // Устанавливаем значения по умолчанию для ComboBox
                TypeComboBox.Items.Add("Ремонт");
                TypeComboBox.Items.Add("Обслуживание");
                TypeComboBox.Items.Add("Консультация");
                TypeComboBox.Items.Add("Экстренный вызов");
                TypeComboBox.SelectedIndex = 0;

                StatusComboBox.Items.Add("Открыта");
                StatusComboBox.Items.Add("В работе");
                StatusComboBox.Items.Add("Закрыта");
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

            // Если статус изменился на "Закрыта", обновляем дату
            if (_originalRequest.status == "Закрыта" && _originalRequest.created_date == null)
            {
                _originalRequest.created_date = DateTime.Now;
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
                created_date = DateTime.Now
            };

            var selectedEmployee = EmployeeComboBox.SelectedItem as EmployeeViewModel;
            if (selectedEmployee != null && selectedEmployee.employee_id > 0)
            {
                newRequest.employee_id = selectedEmployee.employee_id;
            }

            _context.ServiceRequests.Add(newRequest);
        }

        private bool ValidateData()
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
            ErrorText.Text = "";

            var errors = "";

            // Проверка квартиры
            var selectedApartment = ApartmentComboBox.SelectedItem as ApartmentViewModel;
            if (selectedApartment == null || selectedApartment.apartment_id == 0)
                errors += "• Выберите квартиру\n";

            // Проверка типа заявки
            if (TypeComboBox.SelectedItem == null)
                errors += "• Выберите тип заявки\n";

            // Проверка описания
            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
                errors += "• Введите описание\n";

            // Проверка статуса
            if (StatusComboBox.SelectedItem == null)
                errors += "• Выберите статус\n";

            if (!string.IsNullOrEmpty(errors))
            {
                ErrorText.Text = errors;
                ErrorBorder.Visibility = Visibility.Visible;
                return false;
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ServiceRequestsPage());
        }
    }
}