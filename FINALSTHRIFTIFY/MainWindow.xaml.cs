using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace FINALSTHRIFTIFY
{
    public partial class MainWindow : Window
    {
        private int currentPage = 1;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ShowPage(int pageNumber)
        {
            // Hide all pages
            Page1.Visibility = Visibility.Collapsed;
            Page2.Visibility = Visibility.Collapsed;
            Page3.Visibility = Visibility.Collapsed;

            // Show selected page
            switch (pageNumber)
            {
                case 1:
                    Page1.Visibility = Visibility.Visible;
                    break;
                case 2:
                    Page2.Visibility = Visibility.Visible;
                    StartLoadingAnimation();
                    break;
                case 3:
                    Page3.Visibility = Visibility.Visible;
                    break;
            }

            currentPage = pageNumber;
        }

        private void StartLoadingAnimation()
        {
            var storyboard = (Storyboard)FindResource("LoadingAnimation");
            storyboard.Begin();

            // Auto-advance to page 3 after 2 seconds
            Task.Delay(2000).ContinueWith(t =>
            {
                Dispatcher.Invoke(() =>
                {
                    storyboard.Stop();
                    ShowPage(3);
                });
            });
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(2);
        }

        private void SignInSubmit_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter both username and password");
                return;
            }

            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var user = db.Users.FirstOrDefault(u => u.username == username && u.is_deleted == false);

                    if (user == null || !user.is_active)
                    {
                        ShowError("Invalid username or account disabled");
                        return;
                    }

                    if (!VerifyPassword(password, user.password))
                    {
                        ShowError("Invalid password");
                        return;
                    }

                    // Create login session
                    var session = new UserSession
                    {
                        user_id = user.user_id,
                        login_time = DateTime.Now,
                        session_token = Guid.NewGuid().ToString()
                    };
                    db.UserSessions.InsertOnSubmit(session);
                    db.SubmitChanges();

                    // Route to appropriate window
                    if (user.user_type == "Admin" || user.user_type == "Database Administrator")
                    {
                        var adminWindow = new AdminWindow(user);
                        adminWindow.Show();
                    }
                    else
                    {
                        var userWindow = new UserWindow(user);
                        userWindow.Show();
                    }

                    this.Close();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Login error: {ex.Message}");
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UsernamePlaceholder.Visibility = Visibility.Collapsed;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(UsernameTextBox.Text))
            {
                UsernamePlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            PasswordPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(PasswordBox.Password))
            {
                PasswordPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void ClearUsername_Click(object sender, RoutedEventArgs e)
        {
            UsernameTextBox.Clear();
            UsernameTextBox.Focus();
        }

        private void ClearPassword_Click(object sender, RoutedEventArgs e)
        {
            PasswordBox.Clear();
            PasswordBox.Focus();
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Password reset functionality would be implemented here.\n\nFor demo purposes, all accounts use password: 'hello'", 
                "Forgot Password", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
            var registrationWindow = new RegistrationWindow();
            registrationWindow.ShowDialog();
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToUpper();
            }
        }

        private static bool VerifyPassword(string inputPassword, string storedHash)
        {
            var hashOfInput = HashPassword(inputPassword);
            return string.Equals(hashOfInput, storedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}