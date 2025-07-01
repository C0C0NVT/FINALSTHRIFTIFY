using System;
using System.Collections.Generic;
using System.Linq;

namespace FINALSTHRIFTIFY
{
    public class NotificationService
    {
        private static NotificationService _instance;

        public static NotificationService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new NotificationService();
                return _instance;
            }
        }

        private NotificationService() { }

        #region Notification Creation

        public void CreateNotification(int userId, string title, string message, NotificationType type = NotificationType.Info)
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var notification = new Notification
                    {
                        user_id = userId,
                        title = title,
                        message = message,
                        notification_type = type.ToString(),
                        is_read = false,
                        created_at = DateTime.Now
                    };

                    db.Notifications.InsertOnSubmit(notification);
                    db.SubmitChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating notification: {ex.Message}");
            }
        }

        public void CreateWelcomeNotification(int userId, string userName)
        {
            CreateNotification(userId, 
                "🎉 Welcome to Thriftify!", 
                $"Hi {userName}! Welcome to your personal finance journey. Start by creating your first wallet and tracking your expenses.",
                NotificationType.Success);
        }

        public void CreateWalletCreatedNotification(int userId, string walletName)
        {
            CreateNotification(userId,
                "💰 Wallet Created",
                $"Your '{walletName}' wallet has been created successfully! You can now start tracking transactions.",
                NotificationType.Success);
        }

        public void CreateTransactionNotification(int userId, string transactionType, decimal amount, string walletName)
        {
            string emoji = transactionType == "Income" ? "💵" : "💳";
            string action = transactionType == "Income" ? "added to" : "spent from";
            
            CreateNotification(userId,
                $"{emoji} Transaction Recorded",
                $"₱{amount:N2} has been {action} your '{walletName}' wallet.",
                NotificationType.Info);
        }

        #endregion

        #region Gamification Features

        public void CheckAndCreateAchievements(int userId)
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var user = db.Users.FirstOrDefault(u => u.user_id == userId);
                    if (user == null) return;

                    var wallets = db.Wallets.Where(w => w.user_id == userId && w.is_active && !w.is_deleted).ToList();
                    var transactions = db.Transactions
                        .Where(t => t.Wallet.user_id == userId && t.is_active && !t.is_deleted)
                        .ToList();

                    CheckFirstWalletAchievement(userId, wallets);
                    CheckTransactionMilestones(userId, transactions);
                    CheckSavingsGoals(userId, wallets);
                    CheckStreakAchievements(userId, transactions);
                    CheckBudgetAchievements(userId, wallets, transactions);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking achievements: {ex.Message}");
            }
        }

        private void CheckFirstWalletAchievement(int userId, List<Wallet> wallets)
        {
            if (wallets.Count == 1 && !HasNotification(userId, "First Wallet"))
            {
                CreateNotification(userId,
                    "🏆 First Wallet Achievement",
                    "Congratulations! You've created your first wallet. You're on your way to better financial management!",
                    NotificationType.Success);
            }
        }

        private void CheckTransactionMilestones(int userId, List<Transaction> transactions)
        {
            int transactionCount = transactions.Count;
            
            var milestones = new Dictionary<int, string>
            {
                { 1, "🎯 First Transaction!" },
                { 10, "📈 10 Transactions Milestone" },
                { 50, "🚀 50 Transactions Achievement" },
                { 100, "💫 Century Club - 100 Transactions!" },
                { 250, "⭐ Transaction Master - 250 Transactions!" }
            };

            foreach (var milestone in milestones)
            {
                if (transactionCount >= milestone.Key && !HasNotification(userId, milestone.Value))
                {
                    CreateNotification(userId,
                        milestone.Value,
                        $"Amazing! You've recorded {milestone.Key} transactions. Keep tracking to reach your financial goals!",
                        NotificationType.Success);
                }
            }
        }

        private void CheckSavingsGoals(int userId, List<Wallet> wallets)
        {
            decimal totalBalance = wallets.Sum(w => w.current_balance);
            
            var savingsGoals = new Dictionary<decimal, string>
            {
                { 1000, "💰 First ₱1,000 Saved!" },
                { 5000, "🏦 ₱5,000 Savings Milestone" },
                { 10000, "💎 ₱10,000 Achievement" },
                { 25000, "🌟 ₱25,000 Savings Star" },
                { 50000, "👑 ₱50,000 Savings Royalty" }
            };

            foreach (var goal in savingsGoals)
            {
                if (totalBalance >= goal.Key && !HasNotification(userId, goal.Value))
                {
                    CreateNotification(userId,
                        goal.Value,
                        $"Incredible! Your total balance has reached ₱{goal.Key:N2}. You're building great financial habits!",
                        NotificationType.Success);
                }
            }
        }

        private void CheckStreakAchievements(int userId, List<Transaction> transactions)
        {
            if (transactions.Count < 7) return;

            // Check for daily transaction streaks
            var recentDays = transactions
                .Where(t => t.transaction_date >= DateTime.Now.AddDays(-30))
                .GroupBy(t => t.transaction_date.Date)
                .OrderBy(g => g.Key)
                .ToList();

            int consecutiveDays = 0;
            DateTime lastDate = DateTime.MinValue;

            foreach (var dayGroup in recentDays.Reverse())
            {
                if (lastDate == DateTime.MinValue || lastDate.AddDays(-1) == dayGroup.Key)
                {
                    consecutiveDays++;
                    lastDate = dayGroup.Key;
                }
                else
                {
                    break;
                }
            }

            var streakGoals = new Dictionary<int, string>
            {
                { 3, "🔥 3-Day Streak" },
                { 7, "⚡ Weekly Warrior" },
                { 14, "🏃 Two-Week Champion" },
                { 30, "🏆 Monthly Legend" }
            };

            foreach (var streak in streakGoals)
            {
                if (consecutiveDays >= streak.Key && !HasNotification(userId, streak.Value))
                {
                    CreateNotification(userId,
                        streak.Value,
                        $"Fantastic! You've been tracking transactions for {streak.Key} consecutive days. Consistency is key!",
                        NotificationType.Success);
                }
            }
        }

        private void CheckBudgetAchievements(int userId, List<Wallet> wallets, List<Transaction> transactions)
        {
            var thisMonth = DateTime.Now.Month;
            var thisYear = DateTime.Now.Year;
            
            var monthlyExpenses = transactions
                .Where(t => t.transaction_type == "Expense" && 
                           t.transaction_date.Month == thisMonth && 
                           t.transaction_date.Year == thisYear)
                .Sum(t => t.amount);

            var monthlyIncome = transactions
                .Where(t => t.transaction_type == "Income" && 
                           t.transaction_date.Month == thisMonth && 
                           t.transaction_date.Year == thisYear)
                .Sum(t => t.amount);

            // Savings rate achievement
            if (monthlyIncome > 0)
            {
                decimal savingsRate = ((monthlyIncome - monthlyExpenses) / monthlyIncome) * 100;
                
                if (savingsRate >= 20 && !HasNotification(userId, "Savings Rate 20%"))
                {
                    CreateNotification(userId,
                        "🎯 Great Saver - 20% Savings Rate",
                        $"Excellent! You're saving {savingsRate:F1}% of your income this month. Financial experts recommend 20%+!",
                        NotificationType.Success);
                }
                else if (savingsRate >= 10 && !HasNotification(userId, "Savings Rate 10%"))
                {
                    CreateNotification(userId,
                        "💡 Good Saver - 10% Savings Rate",
                        $"Good job! You're saving {savingsRate:F1}% of your income. Try to reach 20% for optimal financial health!",
                        NotificationType.Info);
                }
            }
        }

        #endregion

        #region Budget Alerts

        public void CheckBudgetAlerts(int userId)
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var walletsWithBudgets = db.Wallets
                        .Where(w => w.user_id == userId && w.budget_limit.HasValue && w.is_active && !w.is_deleted)
                        .ToList();

                    foreach (var wallet in walletsWithBudgets)
                    {
                        CheckWalletBudgetAlert(userId, wallet);
                    }

                    CheckMonthlySpendingAlert(userId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking budget alerts: {ex.Message}");
            }
        }

        private void CheckWalletBudgetAlert(int userId, Wallet wallet)
        {
            if (!wallet.budget_limit.HasValue) return;

            decimal spentThisMonth = GetMonthlySpending(wallet.wallet_id);
            decimal budgetUsage = (spentThisMonth / wallet.budget_limit.Value) * 100;

            if (budgetUsage >= 90 && !HasRecentNotification(userId, "Budget Alert 90%", wallet.wallet_name))
            {
                CreateNotification(userId,
                    "⚠️ Budget Alert - 90% Used",
                    $"Warning! You've used {budgetUsage:F1}% of your '{wallet.wallet_name}' budget this month (₱{spentThisMonth:N2} of ₱{wallet.budget_limit:N2}).",
                    NotificationType.Warning);
            }
            else if (budgetUsage >= 75 && !HasRecentNotification(userId, "Budget Alert 75%", wallet.wallet_name))
            {
                CreateNotification(userId,
                    "💡 Budget Notice - 75% Used",
                    $"You've used {budgetUsage:F1}% of your '{wallet.wallet_name}' budget this month. Consider tracking expenses more carefully.",
                    NotificationType.Info);
            }
        }

        private void CheckMonthlySpendingAlert(int userId)
        {
            using (var db = new ThriftifyDataContextDataContext())
            {
                var thisMonth = DateTime.Now.Month;
                var thisYear = DateTime.Now.Year;
                var lastMonth = DateTime.Now.AddMonths(-1);

                var currentMonthSpending = db.Transactions
                    .Where(t => t.Wallet.user_id == userId && 
                               t.transaction_type == "Expense" &&
                               t.transaction_date.Month == thisMonth && 
                               t.transaction_date.Year == thisYear &&
                               t.is_active && !t.is_deleted)
                    .Sum(t => (decimal?)t.amount) ?? 0;

                var lastMonthSpending = db.Transactions
                    .Where(t => t.Wallet.user_id == userId && 
                               t.transaction_type == "Expense" &&
                               t.transaction_date.Month == lastMonth.Month && 
                               t.transaction_date.Year == lastMonth.Year &&
                               t.is_active && !t.is_deleted)
                    .Sum(t => (decimal?)t.amount) ?? 0;

                if (lastMonthSpending > 0)
                {
                    decimal increasePercentage = ((currentMonthSpending - lastMonthSpending) / lastMonthSpending) * 100;
                    
                    if (increasePercentage >= 25 && !HasRecentNotification(userId, "Spending Increase", ""))
                    {
                        CreateNotification(userId,
                            "📊 Spending Increase Alert",
                            $"Your spending has increased by {increasePercentage:F1}% compared to last month. Current: ₱{currentMonthSpending:N2}, Last month: ₱{lastMonthSpending:N2}.",
                            NotificationType.Warning);
                    }
                }
            }
        }

        private decimal GetMonthlySpending(int walletId)
        {
            using (var db = new ThriftifyDataContextDataContext())
            {
                var thisMonth = DateTime.Now.Month;
                var thisYear = DateTime.Now.Year;

                return db.Transactions
                    .Where(t => t.wallet_id == walletId && 
                               t.transaction_type == "Expense" &&
                               t.transaction_date.Month == thisMonth && 
                               t.transaction_date.Year == thisYear &&
                               t.is_active && !t.is_deleted)
                    .Sum(t => (decimal?)t.amount) ?? 0;
            }
        }

        #endregion

        #region Helper Methods

        private bool HasNotification(int userId, string titleContains)
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    return db.Notifications.Any(n => n.user_id == userId && n.title.Contains(titleContains));
                }
            }
            catch
            {
                return false;
            }
        }

        private bool HasRecentNotification(int userId, string titleContains, string messageContains)
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var cutoffDate = DateTime.Now.AddDays(-7); // Check last 7 days
                    return db.Notifications.Any(n => n.user_id == userId && 
                                                     n.title.Contains(titleContains) &&
                                                     n.message.Contains(messageContains) &&
                                                     n.created_at >= cutoffDate);
                }
            }
            catch
            {
                return false;
            }
        }

        public List<Notification> GetRecentNotifications(int userId, int count = 10)
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    return db.Notifications
                        .Where(n => n.user_id == userId)
                        .OrderByDescending(n => n.created_at)
                        .Take(count)
                        .ToList();
                }
            }
            catch
            {
                return new List<Notification>();
            }
        }

        public void MarkNotificationAsRead(int notificationId)
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var notification = db.Notifications.FirstOrDefault(n => n.notification_id == notificationId);
                    if (notification != null)
                    {
                        notification.is_read = true;
                        db.SubmitChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking notification as read: {ex.Message}");
            }
        }

        public void MarkAllNotificationsAsRead(int userId)
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var unreadNotifications = db.Notifications.Where(n => n.user_id == userId && !n.is_read);
                    foreach (var notification in unreadNotifications)
                    {
                        notification.is_read = true;
                    }
                    db.SubmitChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking all notifications as read: {ex.Message}");
            }
        }

        public int GetUnreadNotificationCount(int userId)
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    return db.Notifications.Count(n => n.user_id == userId && !n.is_read);
                }
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Weekly/Monthly Reports

        public void CreateWeeklyReport(int userId)
        {
            try
            {
                using (var db = new ThriftifyDataContextDataContext())
                {
                    var weekStart = DateTime.Now.AddDays(-7);
                    var weeklyTransactions = db.Transactions
                        .Where(t => t.Wallet.user_id == userId && 
                                   t.transaction_date >= weekStart &&
                                   t.is_active && !t.is_deleted)
                        .ToList();

                    if (weeklyTransactions.Any())
                    {
                        var totalIncome = weeklyTransactions.Where(t => t.transaction_type == "Income").Sum(t => t.amount);
                        var totalExpenses = weeklyTransactions.Where(t => t.transaction_type == "Expense").Sum(t => t.amount);
                        var transactionCount = weeklyTransactions.Count;

                        CreateNotification(userId,
                            "📊 Weekly Financial Report",
                            $"This week: {transactionCount} transactions, ₱{totalIncome:N2} income, ₱{totalExpenses:N2} expenses. Net: ₱{(totalIncome - totalExpenses):N2}",
                            NotificationType.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating weekly report: {ex.Message}");
            }
        }

        #endregion
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}