using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FINALSTHRIFTIFY
{
    public partial class AdminWindow : Window
    {
        private User currentUser;
        private string currentUserFilter = "Active";

        public AdminWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            WelcomeText.Text = $"Welcome, {user.display_name ?? user.username}";
            LoadData();
        }

        private void LoadData()
        {
            LoadUsers();
            LoadCategories();
        }

        private void LoadUsers()
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var users = db.Users.Where(u => !u.is_deleted).ToList();

                    // Apply filter
                    switch (currentUserFilter)
                    {
                        case "Active":
                            users = users.Where(u => u.is_active).ToList();
                            break;
                        case "Inactive":
                            users = users.Where(u => !u.is_active).ToList();
                            break;
                        // "All" shows all users (no additional filter)
                    }

                    // Add display properties
                    var userDisplayList = users.Select(u => new UserDisplayModel
                    {
                        user_id = u.user_id,
                        username = u.username,
                        display_name = u.display_name ?? u.username,
                        email = u.email,
                        user_type = u.user_type,
                        is_active = u.is_active,
                        is_deleted = u.is_deleted,
                        created_at = u.created_at,
                        StatusText = u.is_active ? "Active" : "Inactive",
                        StatusColor = u.is_active ? "#8FD398" : "#D84F4E"
                    }).ToList();

                    UsersItemsControl.ItemsSource = userDisplayList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCategories()
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    CategoriesListView.ItemsSource = db.Categories.ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading categories: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UserFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (UserFilterComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                currentUserFilter = selectedItem.Content.ToString().Replace(" Users", "");
                LoadUsers();
            }
        }

        private void ViewUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int userId)
            {
                try
                {
                    using (var db = new ThriftifyDataContextDataContext())
                    {
                        var user = db.Users.FirstOrDefault(u => u.user_id == userId);
                        if (user != null)
                        {
                            var walletCount = db.Wallets.Count(w => w.user_id == userId && w.is_active && !w.is_deleted);
                            var transactionCount = db.Transactions
                                .Count(t => t.Wallet.user_id == userId && t.is_active && !t.is_deleted);
                            var lastLogin = db.UserSessions
                                .Where(s => s.user_id == userId)
                                .OrderByDescending(s => s.login_time)
                                .FirstOrDefault()?.login_time;

                            string userInfo = $"User Details:\n\n" +
                                            $"Name: {user.display_name ?? user.username}\n" +
                                            $"Username: {user.username}\n" +
                                            $"Email: {user.email}\n" +
                                            $"Type: {user.user_type}\n" +
                                            $"Status: {(user.is_active ? "Active" : "Inactive")}\n" +
                                            $"Theme: {user.theme_preference}\n" +
                                            $"Wallets: {walletCount}\n" +
                                            $"Transactions: {transactionCount}\n" +
                                            $"Joined: {user.created_at:MMM dd, yyyy}\n" +
                                            $"Last Login: {(lastLogin?.ToString("MMM dd, yyyy HH:mm") ?? "Never")}";

                            MessageBox.Show(userInfo, "User Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading user details: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ToggleUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int userId)
            {
                try
                {
                    using (var db = new ThriftifyDataContextDataContext())
                    {
                        var user = db.Users.FirstOrDefault(u => u.user_id == userId);
                        if (user != null)
                        {
                            // Prevent admin from disabling themselves
                            if (user.user_id == currentUser.user_id)
                            {
                                MessageBox.Show("You cannot disable your own account.", "Access Denied", 
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            string action = user.is_active ? "disable" : "enable";
                            var result = MessageBox.Show($"Are you sure you want to {action} user '{user.username}'?", 
                                "Confirm Action", MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                user.is_active = !user.is_active;
                                user.updated_at = DateTime.Now;
                                db.SubmitChanges();
                                LoadUsers();

                                MessageBox.Show($"User {action}d successfully!", "Success", 
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating user: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int userId)
            {
                try
                {
                    using (var db = new ThriftifyDataContextDataContext())
                    {
                        var user = db.Users.FirstOrDefault(u => u.user_id == userId);
                        if (user != null)
                        {
                            // Prevent admin from deleting themselves
                            if (user.user_id == currentUser.user_id)
                            {
                                MessageBox.Show("You cannot delete your own account.", "Access Denied", 
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            // Prevent deleting other admins (unless user is Database Administrator)
                            if (user.user_type == "Admin" || user.user_type == "Database Administrator")
                            {
                                if (currentUser.user_type != "Database Administrator")
                                {
                                    MessageBox.Show("Only Database Administrators can delete admin accounts.", 
                                        "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                            }

                            var result = MessageBox.Show(
                                $"Are you sure you want to delete user '{user.username}'?\n\n" +
                                "This will soft-delete the user and all their data.\n" +
                                "The user will be removed from this view but data will remain in the database.",
                                "Confirm Delete User", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                            if (result == MessageBoxResult.Yes)
                            {
                                // Soft delete user and their data
                                user.is_deleted = true;
                                user.is_active = false;
                                user.updated_at = DateTime.Now;

                                // Soft delete user's wallets
                                var userWallets = db.Wallets.Where(w => w.user_id == userId);
                                foreach (var wallet in userWallets)
                                {
                                    wallet.is_deleted = true;
                                    wallet.updated_at = DateTime.Now;
                                }

                                // Soft delete user's transactions
                                var userTransactions = db.Transactions
                                    .Where(t => t.Wallet.user_id == userId);
                                foreach (var transaction in userTransactions)
                                {
                                    transaction.is_deleted = true;
                                    transaction.updated_at = DateTime.Now;
                                }

                                db.SubmitChanges();
                                LoadUsers();

                                MessageBox.Show("User deleted successfully!", "Success", 
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting user: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName.Text) || NewCategoryType.SelectedItem == null)
            {
                CategoryErrorText.Text = "Please fill all fields";
                CategoryErrorText.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var categoryType = ((ComboBoxItem)NewCategoryType.SelectedItem).Content.ToString();

                    var newCategory = new Category
                    {
                        category_name = NewCategoryName.Text,
                        category_type = categoryType
                    };

                    db.Categories.InsertOnSubmit(newCategory);
                    db.SubmitChanges();

                    NewCategoryName.Text = "";
                    NewCategoryType.SelectedItem = null;
                    CategoryErrorText.Visibility = Visibility.Collapsed;

                    LoadData();
                }
            }
            catch (Exception ex)
            {
                CategoryErrorText.Text = $"Error: {ex.Message}";
                CategoryErrorText.Visibility = Visibility.Visible;
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Update session logout time
                    using (var db = new ThriftifyDataContextDataContext())
                    {
                        var lastSession = db.UserSessions
                            .Where(s => s.user_id == currentUser.user_id && s.logout_time == null)
                            .OrderByDescending(s => s.login_time)
                            .FirstOrDefault();
                        
                        if (lastSession != null)
                        {
                            lastSession.logout_time = DateTime.Now;
                            db.SubmitChanges();
                        }
                    }
                }
                catch
                {
                    // Ignore session update errors during logout
                }

                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
        }
    }

    // Helper class for user display in admin panel
    public class UserDisplayModel
    {
        public int user_id { get; set; }
        public string username { get; set; }
        public string display_name { get; set; }
        public string email { get; set; }
        public string user_type { get; set; }
        public bool is_active { get; set; }
        public bool is_deleted { get; set; }
        public DateTime created_at { get; set; }
        public string StatusText { get; set; }
        public string StatusColor { get; set; }
    }
}