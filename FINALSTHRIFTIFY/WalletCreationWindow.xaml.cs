using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Globalization;

namespace FINALSTHRIFTIFY
{
    public partial class WalletCreationWindow : Window
    {
        private User currentUser;
        private Wallet editingWallet;
        private bool isEditMode;
        private string selectedWalletType = "Cash";
        private string selectedBackgroundColor = "#8FD398";
        private string selectedTextColor = "#000000";

        private readonly Dictionary<string, string> walletTypeColors = new Dictionary<string, string>
        {
            { "Cash", "#8FD398" },
            { "GCash", "#3B79BF" },
            { "Maya", "#FF6B35" },
            { "Bank", "#1E3A8A" },
            { "Custom", "#45A176" }
        };

        private readonly List<string> backgroundColors = new List<string>
        {
            "#8FD398", "#3B79BF", "#FF6B35", "#1E3A8A", "#45A176",
            "#E74C3C", "#9B59B6", "#F39C12", "#27AE60", "#2980B9",
            "#E67E22", "#1ABC9C", "#34495E", "#F1C40F", "#95A5A6"
        };

        private readonly Dictionary<string, string> textColorMap = new Dictionary<string, string>
        {
            { "Black", "#000000" },
            { "White", "#FFFFFF" },
            { "DarkGreen", "#2C5F3E" }
        };

        // Constructor for adding new wallet
        public WalletCreationWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            isEditMode = false;
            InitializeWindow();
        }

        // Constructor for editing existing wallet
        public WalletCreationWindow(User user, Wallet wallet)
        {
            InitializeComponent();
            currentUser = user;
            editingWallet = wallet;
            isEditMode = true;
            InitializeWindow();
            LoadWalletData();
        }

        private void InitializeWindow()
        {
            HeaderText.Text = isEditMode ? "Edit Wallet" : "Add Wallet";
            
            if (isEditMode)
            {
                DeleteButton.Visibility = Visibility.Visible;
                SaveButton.Content = "UPDATE WALLET";
            }

            InitializeColorPickers();
            UpdateWalletTypeSelection("Cash");
            UpdatePreview();
        }

        private void InitializeColorPickers()
        {
            // Add background color buttons
            BackgroundColorPanel.Children.Clear();
            foreach (string color in backgroundColors)
            {
                var button = new Button
                {
                    Style = (Style)FindResource("ColorButton"),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                    Tag = color
                };
                button.Click += BackgroundColor_Click;
                BackgroundColorPanel.Children.Add(button);
            }

            // Set default selections
            UpdateBackgroundColorSelection(selectedBackgroundColor);
            UpdateTextColorSelection("Black");
        }

        private void LoadWalletData()
        {
            if (editingWallet == null) return;

            WalletNameTextBox.Text = editingWallet.wallet_name;
            InitialBalanceTextBox.Text = editingWallet.current_balance.ToString("F2");
            selectedWalletType = editingWallet.wallet_type ?? "Cash";
            selectedBackgroundColor = editingWallet.background_color ?? "#8FD398";
            selectedTextColor = editingWallet.text_color ?? "#000000";

            // Hide placeholders
            WalletNamePlaceholder.Visibility = Visibility.Collapsed;
            InitialBalancePlaceholder.Visibility = Visibility.Collapsed;

            // Update UI selections
            UpdateWalletTypeSelection(selectedWalletType);
            UpdateBackgroundColorSelection(selectedBackgroundColor);
            
            // Find and select text color
            string textColorName = "Black";
            foreach (var kvp in textColorMap)
            {
                if (kvp.Value.Equals(selectedTextColor, StringComparison.OrdinalIgnoreCase))
                {
                    textColorName = kvp.Key;
                    break;
                }
            }
            UpdateTextColorSelection(textColorName);

            UpdatePreview();
        }

        #region Event Handlers

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void WalletType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                string walletType = button.Tag.ToString();
                UpdateWalletTypeSelection(walletType);
                
                // Auto-set default color for wallet type
                if (walletTypeColors.ContainsKey(walletType))
                {
                    selectedBackgroundColor = walletTypeColors[walletType];
                    UpdateBackgroundColorSelection(selectedBackgroundColor);
                }
                
                UpdatePreview();
            }
        }

        private void BackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                string color = button.Tag.ToString();
                UpdateBackgroundColorSelection(color);
                UpdatePreview();
            }
        }

        private void TextColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                string colorName = button.Tag.ToString();
                UpdateTextColorSelection(colorName);
                UpdatePreview();
            }
        }

        private void WalletNameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            WalletNamePlaceholder.Visibility = Visibility.Collapsed;
        }

        private void WalletNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(WalletNameTextBox.Text))
            {
                WalletNamePlaceholder.Visibility = Visibility.Visible;
            }
            UpdatePreview();
        }

        private void InitialBalanceTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            InitialBalancePlaceholder.Visibility = Visibility.Collapsed;
        }

        private void InitialBalanceTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InitialBalanceTextBox.Text))
            {
                InitialBalancePlaceholder.Visibility = Visibility.Visible;
            }
            UpdatePreview();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInputs())
            {
                if (isEditMode)
                {
                    UpdateWallet();
                }
                else
                {
                    CreateWallet();
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete the wallet '{editingWallet?.wallet_name}'?\n\n" +
                "This action cannot be undone and will also delete all associated transactions.",
                "Confirm Delete Wallet",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                DeleteWallet();
            }
        }

        #endregion

        #region UI Update Methods

        private void UpdateWalletTypeSelection(string walletType)
        {
            selectedWalletType = walletType;

            // Reset all wallet type button borders
            CashWalletButton.BorderBrush = new SolidColorBrush(Colors.LightGray);
            GCashWalletButton.BorderBrush = new SolidColorBrush(Colors.LightGray);
            MayaWalletButton.BorderBrush = new SolidColorBrush(Colors.LightGray);
            BankWalletButton.BorderBrush = new SolidColorBrush(Colors.LightGray);
            CustomWalletButton.BorderBrush = new SolidColorBrush(Colors.LightGray);

            // Highlight selected wallet type
            Button selectedButton = null;
            switch (walletType)
            {
                case "Cash": selectedButton = CashWalletButton; break;
                case "GCash": selectedButton = GCashWalletButton; break;
                case "Maya": selectedButton = MayaWalletButton; break;
                case "Bank": selectedButton = BankWalletButton; break;
                case "Custom": selectedButton = CustomWalletButton; break;
            }

            if (selectedButton != null)
            {
                selectedButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#45A176"));
                selectedButton.BorderThickness = new Thickness(3);
            }
        }

        private void UpdateBackgroundColorSelection(string color)
        {
            selectedBackgroundColor = color;

            // Reset all background color button borders
            foreach (Button button in BackgroundColorPanel.Children.OfType<Button>())
            {
                button.BorderBrush = new SolidColorBrush(Colors.Transparent);
            }

            // Highlight selected color
            var selectedButton = BackgroundColorPanel.Children.OfType<Button>()
                .FirstOrDefault(b => b.Tag?.ToString() == color);
            
            if (selectedButton != null)
            {
                selectedButton.BorderBrush = new SolidColorBrush(Colors.White);
            }
        }

        private void UpdateTextColorSelection(string colorName)
        {
            if (textColorMap.ContainsKey(colorName))
            {
                selectedTextColor = textColorMap[colorName];
            }

            // Reset all text color button borders
            BlackTextButton.BorderBrush = new SolidColorBrush(Colors.Transparent);
            WhiteTextButton.BorderBrush = new SolidColorBrush(Colors.Transparent);
            DarkGreenTextButton.BorderBrush = new SolidColorBrush(Colors.Transparent);

            // Highlight selected text color
            Button selectedButton = null;
            switch (colorName)
            {
                case "Black": selectedButton = BlackTextButton; break;
                case "White": selectedButton = WhiteTextButton; break;
                case "DarkGreen": selectedButton = DarkGreenTextButton; break;
            }

            if (selectedButton != null)
            {
                selectedButton.BorderBrush = new SolidColorBrush(Colors.Gold);
            }
        }

        private void UpdatePreview()
        {
            // Update preview background color
            PreviewBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(selectedBackgroundColor));

            // Update preview text color
            var textColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(selectedTextColor));
            PreviewWalletName.Foreground = textColor;
            PreviewWalletType.Foreground = textColor;

            // Update preview text content
            string walletName = string.IsNullOrWhiteSpace(WalletNameTextBox.Text) ? "My Wallet" : WalletNameTextBox.Text;
            PreviewWalletName.Text = walletName;
            PreviewWalletType.Text = selectedWalletType;

            // Update preview balance
            decimal balance = 0;
            if (decimal.TryParse(InitialBalanceTextBox.Text, out balance))
            {
                PreviewBalance.Text = balance.ToString("C", new CultureInfo("en-PH"));
            }
            else
            {
                PreviewBalance.Text = "₱0.00";
            }
            PreviewBalance.Foreground = textColor;

            // Update other preview text elements
            var balanceLabel = PreviewBorder.Children.OfType<StackPanel>().FirstOrDefault()?.Children.OfType<TextBlock>().ElementAtOrDefault(2);
            if (balanceLabel != null)
            {
                balanceLabel.Foreground = textColor;
            }
        }

        #endregion

        #region Validation and Database Operations

        private bool ValidateInputs()
        {
            HideMessages();

            if (string.IsNullOrWhiteSpace(WalletNameTextBox.Text))
            {
                ShowError("Please enter a wallet name.");
                return false;
            }

            if (WalletNameTextBox.Text.Length > 50)
            {
                ShowError("Wallet name must be 50 characters or less.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(InitialBalanceTextBox.Text))
            {
                if (!decimal.TryParse(InitialBalanceTextBox.Text, out decimal balance))
                {
                    ShowError("Please enter a valid balance amount.");
                    return false;
                }

                if (balance < 0)
                {
                    ShowError("Initial balance cannot be negative.");
                    return false;
                }
            }

            // Check for duplicate wallet names (excluding current wallet if editing)
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var existingWallet = db.Wallets.FirstOrDefault(w => 
                        w.user_id == currentUser.user_id && 
                        w.wallet_name.ToLower() == WalletNameTextBox.Text.Trim().ToLower() &&
                        w.is_active && !w.is_deleted &&
                        (!isEditMode || w.wallet_id != editingWallet.wallet_id));

                    if (existingWallet != null)
                    {
                        ShowError("A wallet with this name already exists.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Validation error: {ex.Message}");
                return false;
            }

            return true;
        }

        private void CreateWallet()
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    decimal initialBalance = 0;
                    decimal.TryParse(InitialBalanceTextBox.Text, out initialBalance);

                    var newWallet = new Wallet
                    {
                        user_id = currentUser.user_id,
                        wallet_name = WalletNameTextBox.Text.Trim(),
                        wallet_type = selectedWalletType,
                        current_balance = initialBalance,
                        initial_balance = initialBalance,
                        background_color = selectedBackgroundColor,
                        text_color = selectedTextColor,
                        is_active = true,
                        is_deleted = false,
                        created_at = DateTime.Now
                    };

                    db.Wallets.InsertOnSubmit(newWallet);
                    db.SubmitChanges();

                    // Create initial balance transaction if amount > 0
                    if (initialBalance > 0)
                    {
                        var initialTransaction = new Transaction
                        {
                            wallet_id = newWallet.wallet_id,
                            transaction_type = "Income",
                            amount = initialBalance,
                            description = "Initial wallet balance",
                            transaction_date = DateTime.Now,
                            balance_after_transaction = initialBalance,
                            is_active = true,
                            is_deleted = false
                        };

                        db.Transactions.InsertOnSubmit(initialTransaction);
                        db.SubmitChanges();
                    }

                    // Create notification
                    var notification = new Notification
                    {
                        user_id = currentUser.user_id,
                        title = "Wallet Created",
                        message = $"Your new '{newWallet.wallet_name}' wallet has been created successfully!",
                        notification_type = "Success",
                        is_read = false,
                        created_at = DateTime.Now
                    };

                    db.Notifications.InsertOnSubmit(notification);
                    db.SubmitChanges();

                    ShowSuccess("Wallet created successfully!");
                    
                    // Close dialog after a brief delay
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1.5)
                    };
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        DialogResult = true;
                        Close();
                    };
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error creating wallet: {ex.Message}");
            }
        }

        private void UpdateWallet()
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var wallet = db.Wallets.FirstOrDefault(w => w.wallet_id == editingWallet.wallet_id);
                    if (wallet == null)
                    {
                        ShowError("Wallet not found.");
                        return;
                    }

                    decimal newBalance = 0;
                    decimal.TryParse(InitialBalanceTextBox.Text, out newBalance);

                    // Update wallet properties
                    wallet.wallet_name = WalletNameTextBox.Text.Trim();
                    wallet.wallet_type = selectedWalletType;
                    wallet.background_color = selectedBackgroundColor;
                    wallet.text_color = selectedTextColor;
                    wallet.updated_at = DateTime.Now;

                    // Handle balance change
                    if (wallet.current_balance != newBalance)
                    {
                        decimal difference = newBalance - wallet.current_balance;
                        wallet.current_balance = newBalance;

                        // Create adjustment transaction
                        var adjustmentTransaction = new Transaction
                        {
                            wallet_id = wallet.wallet_id,
                            transaction_type = difference >= 0 ? "Income" : "Expense",
                            amount = Math.Abs(difference),
                            description = "Wallet balance adjustment",
                            transaction_date = DateTime.Now,
                            balance_after_transaction = newBalance,
                            is_active = true,
                            is_deleted = false
                        };

                        db.Transactions.InsertOnSubmit(adjustmentTransaction);
                    }

                    db.SubmitChanges();

                    ShowSuccess("Wallet updated successfully!");
                    
                    // Close dialog after a brief delay
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1.5)
                    };
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        DialogResult = true;
                        Close();
                    };
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error updating wallet: {ex.Message}");
            }
        }

        private void DeleteWallet()
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var wallet = db.Wallets.FirstOrDefault(w => w.wallet_id == editingWallet.wallet_id);
                    if (wallet == null)
                    {
                        ShowError("Wallet not found.");
                        return;
                    }

                    // Soft delete wallet
                    wallet.is_deleted = true;
                    wallet.updated_at = DateTime.Now;

                    // Soft delete all transactions associated with this wallet
                    var transactions = db.Transactions.Where(t => t.wallet_id == wallet.wallet_id);
                    foreach (var transaction in transactions)
                    {
                        transaction.is_deleted = true;
                        transaction.updated_at = DateTime.Now;
                    }

                    db.SubmitChanges();

                    MessageBox.Show("Wallet deleted successfully!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error deleting wallet: {ex.Message}");
            }
        }

        #endregion

        #region Message Display

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

        #endregion
    }
}