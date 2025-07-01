using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Globalization;

namespace FINALSTHRIFTIFY
{
    public partial class UserWindow : Window
    {
        private User currentUser;
        private readonly CurrencyService _currencyService;
        private int currentTab = 0;
        private readonly List<Grid> pages;
        private readonly List<(TextBlock icon, TextBlock label)> navElements;

        public UserWindow(User user)
        {
            InitializeComponent();
            currentUser = user;

            WelcomeText.Text = $"Welcome, {user.display_name ?? user.username}";
            _currencyService = new CurrencyService();
            
            // Initialize page navigation
            pages = new List<Grid> { HomePage, HistoryPage, AddPage, BalancePage, SettingsPage };
            navElements = new List<(TextBlock, TextBlock)>
            {
                (HomeIcon, HomeLabel),
                (HistoryIcon, HistoryLabel),
                (AddIcon, AddLabel),
                (BalanceIcon, BalanceLabel),
                (SettingsIcon, SettingsLabel)
            };

            // Initialize theme
            InitializeTheme();
            LoadData();
            UpdateNavigationHighlight();
        }

        private void InitializeTheme()
        {
            // Set theme based on user preference
            if (currentUser.theme_preference == "Dark")
            {
                ThemeManager.Instance.SetTheme(true);
                DarkThemeRadio.IsChecked = true;
            }
            else
            {
                ThemeManager.Instance.SetTheme(false);
                LightThemeRadio.IsChecked = true;
            }

            // Apply theme to window
            ThemeManager.Instance.ApplyThemeToWindow(this);

            // Set profile information
            ProfileNameText.Text = currentUser.display_name ?? currentUser.username;
            ProfileEmailText.Text = currentUser.email;
        }

        private void LoadData()
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    // Load wallets
                    var wallets = db.Wallets
                        .Where(w => w.user_id == currentUser.user_id && w.is_active == true && w.is_deleted == false)
                        .ToList();

                    WalletsItemsControl.ItemsSource = wallets;

                    // Update wallet summary
                    var totalBalance = wallets.Sum(w => w.current_balance);
                    TotalBalanceText.Text = totalBalance.ToString("C", new CultureInfo("en-PH"));
                    WalletCountText.Text = $"{wallets.Count} Wallet{(wallets.Count != 1 ? "s" : "")}";

                    // Load transactions for history
                    var transactions = db.Transactions
                        .Where(t => t.Wallet.user_id == currentUser.user_id && t.is_active == true && t.is_deleted == false)
                        .OrderByDescending(t => t.transaction_date)
                        .Take(50)
                        .ToList();

                    TransactionsItemsControl.ItemsSource = transactions;
                    HistoryDateText.Text = $"As of {DateTime.Now:MMMM dd, yyyy}";

                    // Load dropdown data
                    WalletComboBox.ItemsSource = wallets;
                    AdjustWalletComboBox.ItemsSource = wallets;

                    var categories = db.Categories.ToList();
                    CategoryComboBox.ItemsSource = categories;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Navigation
        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && int.TryParse(button.Tag.ToString(), out int tabIndex))
            {
                SwitchToTab(tabIndex);
            }
        }

        private void SwitchToTab(int tabIndex)
        {
            if (tabIndex == currentTab || tabIndex < 0 || tabIndex >= pages.Count) return;

            // Hide current page
            pages[currentTab].Visibility = Visibility.Collapsed;

            // Show new page with animation
            pages[tabIndex].Visibility = Visibility.Visible;
            AnimatePageTransition(pages[tabIndex]);

            currentTab = tabIndex;
            UpdateNavigationHighlight();

            // Refresh data when switching to certain tabs
            if (tabIndex == 0) // Home
            {
                LoadData();
            }
            else if (tabIndex == 1) // History
            {
                LoadTransactionHistory();
            }
        }

        private void AnimatePageTransition(Grid page)
        {
            var transform = page.RenderTransform as TranslateTransform;
            if (transform == null)
            {
                transform = new TranslateTransform();
                page.RenderTransform = transform;
            }

            var slideAnimation = new DoubleAnimation
            {
                From = 344,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            transform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
        }

        private void UpdateNavigationHighlight()
        {
            var activeColor = (System.Windows.Media.SolidColorBrush)FindResource("ActiveTabColor");
            var inactiveColor = (System.Windows.Media.SolidColorBrush)FindResource("InactiveTabColor");

            for (int i = 0; i < navElements.Count; i++)
            {
                var color = i == currentTab ? activeColor : inactiveColor;
                navElements[i].icon.Foreground = color;
                navElements[i].label.Foreground = color;
            }
        }
        #endregion

        #region Wallet Management
        private void AddWallet_Click(object sender, RoutedEventArgs e)
        {
            var walletWindow = new WalletCreationWindow(currentUser);
            if (walletWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void WalletCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int walletId)
            {
                // Show wallet details animation or navigate to wallet details
                MessageBox.Show($"Wallet details for ID: {walletId}\n\nFeature: Detailed wallet view coming soon!", 
                    "Wallet Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditWallet_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int walletId)
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var wallet = db.Wallets.FirstOrDefault(w => w.wallet_id == walletId);
                    if (wallet != null)
                    {
                        var walletWindow = new WalletCreationWindow(currentUser, wallet);
                        if (walletWindow.ShowDialog() == true)
                        {
                            LoadData();
                        }
                    }
                }
            }
        }

        private void AddFunds_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int walletId)
            {
                var dialog = new InputDialog("Add Funds", "Enter amount to add:");
                if (dialog.ShowDialog() == true)
                {
                    if (decimal.TryParse(dialog.ResponseText, out decimal amount) && amount > 0)
                    {
                        try
                        {
                            using (var db = new ThriftifyDataContextDataContext())
                            {
                                var wallet = db.Wallets.FirstOrDefault(w => w.wallet_id == walletId);
                                if (wallet != null)
                                {
                                    // Create transaction
                                    var transaction = new Transaction
                                    {
                                        wallet_id = walletId,
                                        transaction_type = "Income",
                                        amount = amount,
                                        description = "Manual fund addition",
                                        transaction_date = DateTime.Now,
                                        is_active = true,
                                        is_deleted = false
                                    };

                                    wallet.current_balance += amount;
                                    transaction.balance_after_transaction = wallet.current_balance;

                                    db.Transactions.InsertOnSubmit(transaction);
                                    db.SubmitChanges();

                                    LoadData();
                                    MessageBox.Show("Funds added successfully!", "Success", 
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error adding funds: {ex.Message}", "Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please enter a valid amount.", "Invalid Input", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void Spend_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int walletId)
            {
                var dialog = new InputDialog("Spend Money", "Enter amount to spend:");
                if (dialog.ShowDialog() == true)
                {
                    if (decimal.TryParse(dialog.ResponseText, out decimal amount) && amount > 0)
                    {
                        try
                        {
                            using (var db = new ThriftifyDataContextDataContext())
                            {
                                var wallet = db.Wallets.FirstOrDefault(w => w.wallet_id == walletId);
                                if (wallet != null)
                                {
                                    if (wallet.current_balance < amount)
                                    {
                                        MessageBox.Show("Insufficient funds in wallet!", "Error", 
                                            MessageBoxButton.OK, MessageBoxImage.Warning);
                                        return;
                                    }

                                    // Create transaction
                                    var transaction = new Transaction
                                    {
                                        wallet_id = walletId,
                                        transaction_type = "Expense",
                                        amount = amount,
                                        description = "Manual expense",
                                        transaction_date = DateTime.Now,
                                        is_active = true,
                                        is_deleted = false
                                    };

                                    wallet.current_balance -= amount;
                                    transaction.balance_after_transaction = wallet.current_balance;

                                    db.Transactions.InsertOnSubmit(transaction);
                                    db.SubmitChanges();

                                    LoadData();
                                    MessageBox.Show("Expense recorded successfully!", "Success", 
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error recording expense: {ex.Message}", "Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please enter a valid amount.", "Invalid Input", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }
        #endregion

        #region Transaction Management
        private void LoadTransactionHistory()
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var transactions = db.Transactions
                        .Where(t => t.Wallet.user_id == currentUser.user_id && t.is_active == true && t.is_deleted == false)
                        .OrderByDescending(t => t.transaction_date)
                        .ToList();

                    TransactionsItemsControl.ItemsSource = transactions;
                    HistoryDateText.Text = $"As of {DateTime.Now:MMMM dd, yyyy}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading transaction history: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteTransaction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int transactionId)
            {
                var result = MessageBox.Show("Are you sure you want to delete this transaction?", 
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var db = new ThriftifyDataContextDataContext())
                        {
                            var transaction = db.Transactions.FirstOrDefault(t => t.transaction_id == transactionId);
                            if (transaction != null)
                            {
                                // Soft delete
                                transaction.is_deleted = true;
                                transaction.updated_at = DateTime.Now;
                                
                                // Reverse the transaction effect on wallet balance
                                var wallet = transaction.Wallet;
                                if (transaction.transaction_type == "Income")
                                {
                                    wallet.current_balance -= transaction.amount;
                                }
                                else
                                {
                                    wallet.current_balance += transaction.amount;
                                }

                                db.SubmitChanges();
                                LoadTransactionHistory();
                                LoadData(); // Refresh wallet balances
                                
                                MessageBox.Show("Transaction deleted successfully!", "Success", 
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting transaction: {ex.Message}", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        #endregion

        #region Add Transaction
        private void AddTransaction_Click(object sender, RoutedEventArgs e)
        {
            HideTransactionMessages();

            if (!ValidateTransactionInputs()) return;

            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var selectedWallet = (Wallet)WalletComboBox.SelectedItem;
                    var transactionType = ((ComboBoxItem)TransactionTypeComboBox.SelectedItem).Content.ToString();
                    var selectedCategory = (Category)CategoryComboBox.SelectedItem;
                    var amount = decimal.Parse(AmountTextBox.Text);

                    // Check for sufficient funds if expense
                    if (transactionType == "Expense" && selectedWallet.current_balance < amount)
                    {
                        ShowTransactionError("Insufficient funds in selected wallet!");
                        return;
                    }

                    var newTransaction = new Transaction
                    {
                        wallet_id = selectedWallet.wallet_id,
                        category_id = selectedCategory.category_id,
                        transaction_type = transactionType,
                        amount = amount,
                        description = DescriptionTextBox.Text,
                        transaction_date = DateTime.Now,
                        is_active = true,
                        is_deleted = false
                    };

                    // Update wallet balance
                    if (transactionType == "Income")
                    {
                        selectedWallet.current_balance += amount;
                    }
                    else
                    {
                        selectedWallet.current_balance -= amount;
                    }

                    newTransaction.balance_after_transaction = selectedWallet.current_balance;

                    db.Transactions.InsertOnSubmit(newTransaction);
                    db.SubmitChanges();

                    // Clear form
                    ClearTransactionForm();
                    LoadData();

                    ShowTransactionSuccess("Transaction added successfully!");
                }
            }
            catch (Exception ex)
            {
                ShowTransactionError($"Error adding transaction: {ex.Message}");
            }
        }

        private bool ValidateTransactionInputs()
        {
            if (WalletComboBox.SelectedItem == null)
            {
                ShowTransactionError("Please select a wallet.");
                return false;
            }

            if (TransactionTypeComboBox.SelectedItem == null)
            {
                ShowTransactionError("Please select a transaction type.");
                return false;
            }

            if (CategoryComboBox.SelectedItem == null)
            {
                ShowTransactionError("Please select a category.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(AmountTextBox.Text))
            {
                ShowTransactionError("Please enter an amount.");
                return false;
            }

            if (!decimal.TryParse(AmountTextBox.Text, out decimal amount) || amount <= 0)
            {
                ShowTransactionError("Please enter a valid amount greater than 0.");
                return false;
            }

            return true;
        }

        private void ClearTransactionForm()
        {
            WalletComboBox.SelectedItem = null;
            TransactionTypeComboBox.SelectedItem = null;
            CategoryComboBox.SelectedItem = null;
            AmountTextBox.Clear();
            DescriptionTextBox.Clear();
        }

        private void ShowTransactionError(string message)
        {
            TransactionErrorText.Text = message;
            TransactionErrorText.Visibility = Visibility.Visible;
            TransactionSuccessText.Visibility = Visibility.Collapsed;
        }

        private void ShowTransactionSuccess(string message)
        {
            TransactionSuccessText.Text = message;
            TransactionSuccessText.Visibility = Visibility.Visible;
            TransactionErrorText.Visibility = Visibility.Collapsed;
        }

        private void HideTransactionMessages()
        {
            TransactionErrorText.Visibility = Visibility.Collapsed;
            TransactionSuccessText.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region Balance Adjustment
        private void SaveAdjustment_Click(object sender, RoutedEventArgs e)
        {
            HideAdjustMessages();

            if (!ValidateAdjustmentInputs()) return;

            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var selectedWallet = (Wallet)AdjustWalletComboBox.SelectedItem;
                    var amount = decimal.Parse(AdjustAmountTextBox.Text);
                    var adjustmentType = IncomeRadio.IsChecked == true ? "Income" : "Expense";

                    // Check for sufficient funds if expense
                    if (adjustmentType == "Expense" && selectedWallet.current_balance < amount)
                    {
                        ShowAdjustError("Insufficient funds in selected wallet!");
                        return;
                    }

                    var adjustment = new WalletAdjustment
                    {
                        wallet_id = selectedWallet.wallet_id,
                        adjustment_type = adjustmentType,
                        adjustment_amount = amount,
                        description = AdjustDescriptionTextBox.Text,
                        adjustment_date = DateTime.Now,
                        is_active = true
                    };

                    // Update wallet balance
                    if (adjustmentType == "Income")
                    {
                        selectedWallet.current_balance += amount;
                    }
                    else
                    {
                        selectedWallet.current_balance -= amount;
                    }

                    db.WalletAdjustments.InsertOnSubmit(adjustment);
                    db.SubmitChanges();

                    // Clear form
                    ClearAdjustmentForm();
                    LoadData();

                    ShowAdjustSuccess("Wallet balance adjusted successfully!");
                }
            }
            catch (Exception ex)
            {
                ShowAdjustError($"Error adjusting balance: {ex.Message}");
            }
        }

        private bool ValidateAdjustmentInputs()
        {
            if (AdjustWalletComboBox.SelectedItem == null)
            {
                ShowAdjustError("Please select a wallet.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(AdjustAmountTextBox.Text))
            {
                ShowAdjustError("Please enter an amount.");
                return false;
            }

            if (!decimal.TryParse(AdjustAmountTextBox.Text, out decimal amount) || amount <= 0)
            {
                ShowAdjustError("Please enter a valid amount greater than 0.");
                return false;
            }

            return true;
        }

        private void ClearAdjustmentForm()
        {
            AdjustWalletComboBox.SelectedItem = null;
            AdjustAmountTextBox.Clear();
            AdjustDescriptionTextBox.Clear();
            IncomeRadio.IsChecked = true;
        }

        private void ShowAdjustError(string message)
        {
            AdjustErrorText.Text = message;
            AdjustErrorText.Visibility = Visibility.Visible;
            AdjustSuccessText.Visibility = Visibility.Collapsed;
        }

        private void ShowAdjustSuccess(string message)
        {
            AdjustSuccessText.Text = message;
            AdjustSuccessText.Visibility = Visibility.Visible;
            AdjustErrorText.Visibility = Visibility.Collapsed;
        }

        private void HideAdjustMessages()
        {
            AdjustErrorText.Visibility = Visibility.Collapsed;
            AdjustSuccessText.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region Currency Converter
        private async void ConvertCurrency_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(AmountToConvert.Text, out decimal phpAmount) &&
                CurrencySelector.SelectedItem is ComboBoxItem selectedCurrency)
            {
                string currencyCode = selectedCurrency.Tag.ToString();

                try
                {
                    // Fixed conversion logic: 1 USD = 56 PHP (approximately)
                    decimal exchangeRate = await _currencyService.GetExchangeRate("PHP", currencyCode);
                    decimal convertedAmount = phpAmount / exchangeRate;

                    ConversionResult.Text = $"₱{phpAmount:N2} = {convertedAmount:N2} {currencyCode}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error converting currency: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid amount and select a currency", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Settings & Theme
        private void SettingsDropdown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                string dropdownType = button.Tag.ToString();
                
                if (dropdownType == "Theme")
                {
                    AnimateDropdown(ThemeDropdown);
                }
                else if (dropdownType == "Currency")
                {
                    AnimateDropdown(CurrencyDropdown);
                }
            }
        }

        private void AnimateDropdown(StackPanel dropdown)
        {
            if (dropdown.Height == 0)
            {
                // Open dropdown
                var openAnimation = (Storyboard)FindResource("DropdownOpenAnimation");
                openAnimation.Begin(dropdown);
            }
            else
            {
                // Close dropdown
                var closeAnimation = (Storyboard)FindResource("DropdownCloseAnimation");
                closeAnimation.Begin(dropdown);
            }
        }

        private void ThemeChanged(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                bool isDark = radioButton == DarkThemeRadio;
                ThemeManager.Instance.SetTheme(isDark);
                ThemeManager.Instance.ApplyThemeToWindow(this);

                // Update user preference in database
                try
                {
                    using (var db = new ThriftifyDataContextDataContext())
                    {
                        var user = db.Users.FirstOrDefault(u => u.user_id == currentUser.user_id);
                        if (user != null)
                        {
                            user.theme_preference = isDark ? "Dark" : "Light";
                            db.SubmitChanges();
                            currentUser = user; // Update current user object
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving theme preference: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ChangeProfilePicture_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Profile picture upload feature coming soon!", "Feature Coming Soon", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditProfileName_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var dialog = new InputDialog("Edit Profile Name", "Enter your new display name:");
            dialog.InputTextBox.Text = currentUser.display_name ?? currentUser.username;
            
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResponseText))
            {
                try
                {
                    using (var db = new ThriftifyDataContextDataContext())
                    {
                        var user = db.Users.FirstOrDefault(u => u.user_id == currentUser.user_id);
                        if (user != null)
                        {
                            user.display_name = dialog.ResponseText.Trim();
                            user.updated_at = DateTime.Now;
                            db.SubmitChanges();
                            
                            currentUser = user;
                            ProfileNameText.Text = user.display_name;
                            WelcomeText.Text = $"Welcome, {user.display_name}";
                            
                            MessageBox.Show("Profile name updated successfully!", "Success", 
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating profile name: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void NotificationSettings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Notification Settings:\n\n✅ Budget alerts enabled\n✅ Transaction confirmations enabled\n✅ Weekly summaries enabled\n\nAdvanced notification settings coming soon!", 
                "Notification Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var notifications = db.Notifications
                        .Where(n => n.user_id == currentUser.user_id)
                        .OrderByDescending(n => n.created_at)
                        .Take(5)
                        .ToList();

                    if (notifications.Any())
                    {
                        string notificationText = "Recent Notifications:\n\n";
                        foreach (var notification in notifications)
                        {
                            string readStatus = notification.is_read ? "✓" : "●";
                            notificationText += $"{readStatus} {notification.title}\n{notification.message}\n{notification.created_at:MMM dd, HH:mm}\n\n";
                        }
                        
                        MessageBox.Show(notificationText, "Notifications", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Mark as read
                        foreach (var notification in notifications)
                        {
                            notification.is_read = true;
                        }
                        db.SubmitChanges();
                    }
                    else
                    {
                        MessageBox.Show("No notifications yet.", "Notifications", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading notifications: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WhatsNew_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("🎉 What's New in Thriftify:\n\n" +
                          "✨ Dark mode support\n" +
                          "💰 Enhanced wallet management\n" +
                          "📊 Improved transaction history\n" +
                          "🔄 Better currency conversion\n" +
                          "🔔 Smart notifications\n" +
                          "🎨 Beautiful animations\n\n" +
                          "Version 2.0 - January 2025", 
                "What's New", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ReportProblem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Report a Problem:\n\n" +
                          "📧 Email: support@thriftify.com\n" +
                          "📱 Phone: +63 123 456 7890\n" +
                          "💬 Chat: Available in app (coming soon)\n" +
                          "🐛 Bug reports welcome!\n\n" +
                          "We appreciate your feedback!", 
                "Report Problem", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TermsOfUse_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Terms of Use - Thriftify\n\n" +
                          "1. Use this app responsibly for personal finance tracking\n" +
                          "2. Your data is stored securely and privately\n" +
                          "3. We don't share your financial information\n" +
                          "4. Report any security concerns immediately\n" +
                          "5. This app is for educational and personal use\n\n" +
                          "Last updated: January 2025", 
                "Terms of Use", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RateApp_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Do you enjoy using Thriftify?\n\n" +
                                       "⭐ Rate us 5 stars if you love the app!\n" +
                                       "📝 Your feedback helps us improve\n" +
                                       "💝 Thank you for using Thriftify!\n\n" +
                                       "Would you like to rate us now?", 
                "Rate Thriftify", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show("⭐⭐⭐⭐⭐\n\nThank you for your 5-star rating!\nYour feedback has been submitted.", 
                    "Rating Submitted", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

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
}