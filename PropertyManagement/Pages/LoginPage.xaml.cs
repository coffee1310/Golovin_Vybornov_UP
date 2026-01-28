using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PropertyManagement.Pages
{
    /// <summary>
    /// Логика взаимодействия для LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        private PropertyManagementEntities db = new PropertyManagementEntities();
        private bool isAuthenticating = false;

        public LoginPage()
        {
            InitializeComponent();
            txtLogin.Focus();
            txtLogin.Text = "admin";
            txtPassword.Password = "admin";
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите логин и пароль");
                return;
            }

            btnLogin.IsEnabled = false;

            try
            {
                using (var db = new PropertyManagementEntities())
                {
                    // Используем LINQ join для получения данных
                    var query = from emp in db.Employees
                                join pos in db.Positions on emp.position_id equals pos.position_id into positionJoin
                                from position in positionJoin.DefaultIfEmpty()
                                where emp.login == login && emp.password_hash == password
                                select new
                                {
                                    emp.employee_id,
                                    emp.full_name,
                                    emp.position,
                                    emp.position_id,
                                    emp.login,
                                    PositionName = position != null ? position.position_name : emp.position
                                };

                    var employee = query.FirstOrDefault();

                    if (employee != null)
                    {
                        Application.Current.Properties["EmployeeId"] = employee.employee_id;
                        Application.Current.Properties["FullName"] = employee.full_name;
                        Application.Current.Properties["Position"] = employee.position;
                        Application.Current.Properties["PositionId"] = employee.position_id;
                        Application.Current.Properties["Login"] = employee.login;
                        Application.Current.Properties["PositionName"] = employee.PositionName;

                        var mainWindow = new MainWindow();
                        mainWindow.Show();
                        Window.GetWindow(this)?.Close();
                    }
                    else
                    {
                        ShowError("Неверный логин или пароль");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
            finally
            {
                btnLogin.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }
    }
}