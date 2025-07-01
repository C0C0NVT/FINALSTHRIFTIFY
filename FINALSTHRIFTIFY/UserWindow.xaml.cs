using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace FINALSTHRIFTIFY
{
    public partial class UserWindow : Window
    {
        private User currentUser;
        private readonly CurrencyService _currencyService;

        public UserWindow(User user)
        {
            InitializeComponent();
            currentUser = user;

            WelcomeText.Text = $"Welcome, {user.username}";
            _currencyService = new CurrencyService();
            LoadData();
        }

        private void LoadData()
        {
            using (var db = new ThriftifyDataContextDataContext())
            {
                var userWallets = db.Wallets
                    .Where(w => w.user_id == currentUser.user_id && w.is_active == true)
                    .ToList();

                var userTransactions = db.Transactions
                    .Where(t => t.Wallet.user_id == currentUser.user_id && t.is_active == true)
                    .OrderByDescending(t => t.transaction_date)
                    .ToList();

                // Overview tab data
                WalletsListView.ItemsSource = userWallets;
                TransactionsListView.ItemsSource = userTransactions.Take(10).ToList();

                // History tab data
                AllTransactionsListView.ItemsSource = userTransactions;

                // Balance tab data
                BalanceWalletsListView.ItemsSource = userWallets;

                // Add transaction tab data
                WalletComboBox.ItemsSource = userWallets;
                CategoryComboBox.ItemsSource = db.Categories.ToList();
            }
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset all navigation buttons to inactive style
            HomeButton.Style = (Style)FindResource("NavButtonStyle");
            HistoryButton.Style = (Style)FindResource("NavButtonStyle");
            AddButton.Style = (Style)FindResource("NavButtonStyle");
            BalanceButton.Style = (Style)FindResource("NavButtonStyle");

            // Set clicked button to active style and switch tabs
            Button clickedButton = sender as Button;
            clickedButton.Style = (Style)FindResource("ActiveNavButtonStyle");

            switch (clickedButton.Name)
            {
                case "HomeButton":
                    MainTabControl.SelectedItem = OverviewTab;
                    break;
                case "HistoryButton":
                    MainTabControl.SelectedItem = HistoryTab;
                    break;
                case "AddButton":
                    MainTabControl.SelectedItem = AddTransactionTab;
                    break;
                case "BalanceButton":
                    MainTabControl.SelectedItem = BalanceTab;
                    break;
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsOverlay.Visibility = Visibility.Visible;
        }

        private void CloseSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsOverlay.Visibility = Visibility.Collapsed;
        }

        private void HelpCenter_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Welcome to the Help Center!\n\nHere you can find answers to frequently asked questions, user guides, and troubleshooting tips.\n\nFor immediate assistance, please contact our support team.", 
                "Help Center", MessageBoxButton.OK, MessageBoxImage.Information);
            SettingsOverlay.Visibility = Visibility.Collapsed;
        }

        private void ReportProblem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Report a Problem\n\nIf you're experiencing any issues with the app, please describe the problem below and our team will investigate.\n\nYou can also contact support at: support@thriftify.com", 
                "Report a Problem", MessageBoxButton.OK, MessageBoxImage.Information);
            SettingsOverlay.Visibility = Visibility.Collapsed;
        }

        private void TermsOfUse_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Terms of Use\n\nBy using Thriftify, you agree to our Terms of Service and Privacy Policy.\n\nKey points:\n• Your data is secure and private\n• Use the app responsibly\n• Report any issues promptly\n\nFull terms available at: www.thriftify.com/terms", 
                "Terms of Use", MessageBoxButton.OK, MessageBoxImage.Information);
            SettingsOverlay.Visibility = Visibility.Collapsed;
        }

        private void RateApp_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Rate Thriftify\n\nAre you enjoying using Thriftify? We'd love to hear your feedback!\n\nWould you like to rate the app in the store?", 
                "Rate the App", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show("Thank you for your support! You would be redirected to the app store to leave a review.", 
                    "Thank You!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            SettingsOverlay.Visibility = Visibility.Collapsed;
        }

        private async void ConvertCurrency_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(AmountToConvert.Text, out decimal phpAmount) &&
                CurrencySelector.SelectedItem is ComboBoxItem selectedCurrency)
            {
                string currencyCode = selectedCurrency.Content.ToString().Split('-')[0].Trim();

                try
                {
                    decimal exchangeRate = await _currencyService.GetExchangeRate("PHP", currencyCode);
                    decimal convertedAmount = phpAmount / exchangeRate;

                    ConversionResult.Text = $"{phpAmount:N2} PHP = {convertedAmount:N2} {currencyCode}";
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

        private void AddFunds_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int walletId)
            {
                AmountToConvert.Focus();
                MessageBox.Show($"Add funds to wallet {walletId}", "Add Funds",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Spend_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int walletId)
            {
                AmountToConvert.Focus();
                MessageBox.Show($"Spend from wallet {walletId}", "Spend",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddWallet_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Add New Wallet", "Enter wallet name:");
            if (dialog.ShowDialog() == true)
            {
                if (!string.IsNullOrWhiteSpace(dialog.ResponseText))
                {
                    using (var db = new ThriftifyDataContextDataContext())
                    {
                        var newWallet = new Wallet
                        {
                            user_id = currentUser.user_id,
                            wallet_name = dialog.ResponseText,
                            current_balance = 0,
                            is_active = true,
                            created_at = DateTime.Now
                        };

                        db.Wallets.InsertOnSubmit(newWallet);
                        db.SubmitChanges();
                        LoadData();
                    }
                }
            }
        }

        private void AddTransaction_Click(object sender, RoutedEventArgs e)
        {
            if (WalletComboBox.SelectedItem == null ||
                TransactionTypeComboBox.SelectedItem == null ||
                CategoryComboBox.SelectedItem == null ||
                string.IsNullOrWhiteSpace(AmountTextBox.Text))
            {
                TransactionErrorText.Text = "Please fill all fields";
                TransactionErrorText.Visibility = Visibility.Visible;
                return;
            }

            if (!decimal.TryParse(AmountTextBox.Text, out decimal amount) || amount <= 0)
            {
                TransactionErrorText.Text = "Please enter a valid amount";
                TransactionErrorText.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var selectedWallet = (Wallet)WalletComboBox.SelectedItem;
                    var transactionType = ((ComboBoxItem)TransactionTypeComboBox.SelectedItem).Content.ToString();
                    var selectedCategory = (Category)CategoryComboBox.SelectedItem;

                    var newTransaction = new Transaction
                    {
                        wallet_id = selectedWallet.wallet_id,
                        category_id = selectedCategory.category_id,
                        transaction_type = transactionType,
                        amount = amount,
                        description = DescriptionTextBox.Text,
                        transaction_date = DateTime.Now,
                        is_active = true
                    };

                    if (transactionType == "Income")
                    {
                        selectedWallet.current_balance += amount;
                    }
                    else
                    {
                        selectedWallet.current_balance -= amount;
                    }

                    db.Transactions.InsertOnSubmit(newTransaction);
                    db.SubmitChanges();

                    AmountTextBox.Text = "";
                    DescriptionTextBox.Text = "";
                    TransactionErrorText.Visibility = Visibility.Collapsed;

                    LoadData();

                    MessageBox.Show("Transaction added successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                TransactionErrorText.Text = $"Error: {ex.Message}";
                TransactionErrorText.Visibility = Visibility.Visible;
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