using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace FINALSTHRIFTIFY
{
    public partial class RegistrationWindow : Window
    {
        public RegistrationWindow()
        {
            InitializeComponent();
        }

        #region Focus Events
        private void UsernameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UsernamePlaceholder.Visibility = Visibility.Collapsed;
        }

        private void UsernameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(UsernameTextBox.Text))
            {
                UsernamePlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void DisplayNameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            DisplayNamePlaceholder.Visibility = Visibility.Collapsed;
        }

        private void DisplayNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(DisplayNameTextBox.Text))
            {
                DisplayNamePlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void EmailTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            EmailPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void EmailTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(EmailTextBox.Text))
            {
                EmailPlaceholder.Visibility = Visibility.Visible;
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

        private void ConfirmPasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ConfirmPasswordPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void ConfirmPasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ConfirmPasswordBox.Password))
            {
                ConfirmPasswordPlaceholder.Visibility = Visibility.Visible;
            }
        }
        #endregion

        #region Clear Events
        private void ClearUsername_Click(object sender, RoutedEventArgs e)
        {
            UsernameTextBox.Clear();
            UsernameTextBox.Focus();
        }

        private void ClearDisplayName_Click(object sender, RoutedEventArgs e)
        {
            DisplayNameTextBox.Clear();
            DisplayNameTextBox.Focus();
        }

        private void ClearEmail_Click(object sender, RoutedEventArgs e)
        {
            EmailTextBox.Clear();
            EmailTextBox.Focus();
        }

        private void ClearPassword_Click(object sender, RoutedEventArgs e)
        {
            PasswordBox.Clear();
            PasswordBox.Focus();
        }

        private void ClearConfirmPassword_Click(object sender, RoutedEventArgs e)
        {
            ConfirmPasswordBox.Clear();
            ConfirmPasswordBox.Focus();
        }
        #endregion

        private void SignUpSubmit_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (!ValidateInputs())
                return;

            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    // Check if username already exists
                    if (db.Users.Any(u => u.username == UsernameTextBox.Text.Trim()))
                    {
                        ShowError("Username already exists. Please choose a different username.");
                        return;
                    }

                    // Check if email already exists
                    if (db.Users.Any(u => u.email == EmailTextBox.Text.Trim()))
                    {
                        ShowError("Email address already exists. Please use a different email.");
                        return;
                    }

                    // Create new user
                    var newUser = new User
                    {
                        username = UsernameTextBox.Text.Trim(),
                        email = EmailTextBox.Text.Trim(),
                        display_name = DisplayNameTextBox.Text.Trim(),
                        password = MainWindow.HashPassword(PasswordBox.Password),
                        user_type = "User",
                        is_active = true,
                        is_deleted = false,
                        theme_preference = "Light",
                        currency_preference = "PHP",
                        notifications_enabled = true,
                        created_at = DateTime.Now
                    };

                    db.Users.InsertOnSubmit(newUser);
                    db.SubmitChanges();

                    // Create default categories for the user if they don't exist
                    if (!db.Categories.Any())
                    {
                        var defaultCategories = new[]
                        {
                            new Category { category_name = "Salary", category_type = "Income", is_system_category = true },
                            new Category { category_name = "Food & Dining", category_type = "Expense", is_system_category = true },
                            new Category { category_name = "Transportation", category_type = "Expense", is_system_category = true },
                            new Category { category_name = "Shopping", category_type = "Expense", is_system_category = true },
                            new Category { category_name = "Entertainment", category_type = "Expense", is_system_category = true }
                        };

                        db.Categories.InsertAllOnSubmit(defaultCategories);
                        db.SubmitChanges();
                    }

                    // Create welcome notification
                    var welcomeNotification = new Notification
                    {
                        user_id = newUser.user_id,
                        title = "Welcome to Thriftify!",
                        message = "Your account has been created successfully. Start by creating your first wallet to track your finances.",
                        notification_type = "Success",
                        is_read = false,
                        created_at = DateTime.Now
                    };

                    db.Notifications.InsertOnSubmit(welcomeNotification);
                    db.SubmitChanges();

                    ShowSuccess("Account created successfully! You can now sign in.");
                    
                    // Clear form after successful registration
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Registration failed: {ex.Message}");
            }
        }

        private bool ValidateInputs()
        {
            HideMessages();

            // Check if all fields are filled
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                ShowError("Username is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(DisplayNameTextBox.Text))
            {
                ShowError("Full name is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                ShowError("Email address is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                ShowError("Password is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ConfirmPasswordBox.Password))
            {
                ShowError("Please confirm your password.");
                return false;
            }

            // Validate username
            if (UsernameTextBox.Text.Length < 3)
            {
                ShowError("Username must be at least 3 characters long.");
                return false;
            }

            if (!Regex.IsMatch(UsernameTextBox.Text, @"^[a-zA-Z0-9_]+$"))
            {
                ShowError("Username can only contain letters, numbers, and underscores.");
                return false;
            }

            // Validate email
            if (!IsValidEmail(EmailTextBox.Text))
            {
                ShowError("Please enter a valid email address.");
                return false;
            }

            // Validate password
            if (PasswordBox.Password.Length < 6)
            {
                ShowError("Password must be at least 6 characters long.");
                return false;
            }

            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                ShowError("Passwords do not match.");
                return false;
            }

            // Check terms agreement
            if (!TermsCheckBox.IsChecked.GetValueOrDefault())
            {
                ShowError("You must agree to the Terms of Service and Privacy Policy.");
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
            SuccessTextBlock.Visibility = Visibility.Collapsed;
        }

        private void ShowSuccess(string message)
        {
            SuccessTextBlock.Text = message;
            SuccessTextBlock.Visibility = Visibility.Visible;
            ErrorTextBlock.Visibility = Visibility.Collapsed;
        }

        private void HideMessages()
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            SuccessTextBlock.Visibility = Visibility.Collapsed;
        }

        private void ClearForm()
        {
            UsernameTextBox.Clear();
            DisplayNameTextBox.Clear();
            EmailTextBox.Clear();
            PasswordBox.Clear();
            ConfirmPasswordBox.Clear();
            TermsCheckBox.IsChecked = false;

            // Show placeholders
            UsernamePlaceholder.Visibility = Visibility.Visible;
            DisplayNamePlaceholder.Visibility = Visibility.Visible;
            EmailPlaceholder.Visibility = Visibility.Visible;
            PasswordPlaceholder.Visibility = Visibility.Visible;
            ConfirmPasswordPlaceholder.Visibility = Visibility.Visible;
        }

        private void Terms_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Terms of Service:\n\n1. Use this app responsibly\n2. Your data is secure with us\n3. Report any bugs or issues\n4. Enjoy managing your finances!", 
                "Terms of Service", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Privacy_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Privacy Policy:\n\n1. We protect your personal information\n2. Your financial data is encrypted\n3. We don't share your data with third parties\n4. You can delete your account anytime", 
                "Privacy Policy", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}