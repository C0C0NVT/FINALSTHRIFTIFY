using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace FINALSTHRIFTIFY
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            UsernameTextBox.Focus();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter both username and password");
                return;
            }

            using (var db = new ThriftifyDataContextDataContext())
            {
                var user = db.Users.FirstOrDefault(u => u.username == username);

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

                if (user.user_type == "Admin")
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