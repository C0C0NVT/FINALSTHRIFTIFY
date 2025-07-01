using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
                WalletsListView.ItemsSource = db.Wallets
                    .Where(w => w.user_id == currentUser.user_id && w.is_active == true)
                    .ToList();

                TransactionsListView.ItemsSource = db.Transactions
                    .Where(t => t.Wallet.user_id == currentUser.user_id && t.is_active == true)
                    .OrderByDescending(t => t.transaction_date)
                    .Take(10)
                    .ToList();

                WalletComboBox.ItemsSource = db.Wallets
                    .Where(w => w.user_id == currentUser.user_id && w.is_active == true)
                    .ToList();

                CategoryComboBox.ItemsSource = db.Categories.ToList();
            }
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