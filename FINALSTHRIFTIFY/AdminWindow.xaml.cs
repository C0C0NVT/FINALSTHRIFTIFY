using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


namespace FINALSTHRIFTIFY
{
    public partial class AdminWindow : Window
    {
        private User currentUser;

        public AdminWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            WelcomeText.Text = $"Welcome, {user.username}";
            LoadData();
        }

        private void LoadData()
        {
            using (var db = new ThriftifyDataContextDataContext())
            {
                UsersListView.ItemsSource = db.Users.ToList();

                CategoriesListView.ItemsSource = db.Categories.ToList();
            }
        }

        private void DisableUser_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            int userId = (int)button.Tag;

            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var user = db.Users.FirstOrDefault(u => u.user_id == userId);
                    if (user != null)
                    {
                        user.is_active = false;
                        db.SubmitChanges();
                        LoadData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error disabling user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnableUser_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            int userId = (int)button.Tag;

            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var user = db.Users.FirstOrDefault(u => u.user_id == userId);
                    if (user != null)
                    {
                        user.is_active = true;
                        db.SubmitChanges();
                        LoadData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error enabling user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}