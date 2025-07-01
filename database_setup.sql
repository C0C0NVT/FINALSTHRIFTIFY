-- Thriftify Database Setup Script
-- Database: FinalProject

USE master;
GO

-- Drop database if exists
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'FinalProject')
    DROP DATABASE FinalProject;
GO

-- Create database
CREATE DATABASE FinalProject;
GO

USE FinalProject;
GO

-- Create Users table with additional fields
CREATE TABLE Users (
    user_id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(50) NOT NULL UNIQUE,
    email NVARCHAR(100) NOT NULL UNIQUE,
    password NVARCHAR(255) NOT NULL,
    user_type NVARCHAR(20) NOT NULL DEFAULT 'User' CHECK (user_type IN ('User', 'Admin', 'Database Administrator')),
    is_active BIT NOT NULL DEFAULT 1,
    is_deleted BIT NOT NULL DEFAULT 0,
    profile_picture NVARCHAR(MAX) NULL,
    display_name NVARCHAR(100) NULL,
    theme_preference NVARCHAR(20) DEFAULT 'Light' CHECK (theme_preference IN ('Light', 'Dark')),
    currency_preference NVARCHAR(10) DEFAULT 'PHP',
    notifications_enabled BIT DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME2 NULL
);

-- Create Categories table
CREATE TABLE Categories (
    category_id INT IDENTITY(1,1) PRIMARY KEY,
    category_name NVARCHAR(50) NOT NULL,
    category_type NVARCHAR(20) NOT NULL CHECK (category_type IN ('Income', 'Expense')),
    is_system_category BIT DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- Create Wallets table with enhanced features
CREATE TABLE Wallets (
    wallet_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    wallet_name NVARCHAR(100) NOT NULL,
    wallet_type NVARCHAR(50) DEFAULT 'Cash' CHECK (wallet_type IN ('Cash', 'GCash', 'Maya', 'Bank', 'Custom')),
    current_balance DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    initial_balance DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    background_color NVARCHAR(10) DEFAULT '#8FD398',
    text_color NVARCHAR(10) DEFAULT '#000000',
    budget_limit DECIMAL(15,2) NULL,
    is_active BIT NOT NULL DEFAULT 1,
    is_deleted BIT NOT NULL DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME2 NULL,
    FOREIGN KEY (user_id) REFERENCES Users(user_id)
);

-- Create Transactions table with soft delete
CREATE TABLE Transactions (
    transaction_id INT IDENTITY(1,1) PRIMARY KEY,
    wallet_id INT NOT NULL,
    category_id INT NULL,
    transaction_type NVARCHAR(10) NOT NULL CHECK (transaction_type IN ('Income', 'Expense')),
    amount DECIMAL(15,2) NOT NULL CHECK (amount > 0),
    description NVARCHAR(255) NULL,
    transaction_date DATETIME2 NOT NULL DEFAULT GETDATE(),
    balance_after_transaction DECIMAL(15,2) NULL,
    is_active BIT NOT NULL DEFAULT 1,
    is_deleted BIT NOT NULL DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME2 NULL,
    FOREIGN KEY (wallet_id) REFERENCES Wallets(wallet_id),
    FOREIGN KEY (category_id) REFERENCES Categories(category_id)
);

-- Create WalletAdjustments table
CREATE TABLE WalletAdjustments (
    adjustment_id INT IDENTITY(1,1) PRIMARY KEY,
    wallet_id INT NOT NULL,
    adjustment_type NVARCHAR(10) NOT NULL CHECK (adjustment_type IN ('Income', 'Expense', 'Correction')),
    adjustment_amount DECIMAL(15,2) NOT NULL CHECK (adjustment_amount > 0),
    adjustment_date DATETIME2 NOT NULL DEFAULT GETDATE(),
    description NVARCHAR(255) NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (wallet_id) REFERENCES Wallets(wallet_id)
);

-- Create User Sessions table for login tracking
CREATE TABLE UserSessions (
    session_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    login_time DATETIME2 NOT NULL DEFAULT GETDATE(),
    logout_time DATETIME2 NULL,
    session_token NVARCHAR(255) NULL,
    ip_address NVARCHAR(45) NULL,
    FOREIGN KEY (user_id) REFERENCES Users(user_id)
);

-- Create Notifications table
CREATE TABLE Notifications (
    notification_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    title NVARCHAR(100) NOT NULL,
    message NVARCHAR(500) NOT NULL,
    notification_type NVARCHAR(20) DEFAULT 'Info' CHECK (notification_type IN ('Info', 'Warning', 'Success', 'Error')),
    is_read BIT DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (user_id) REFERENCES Users(user_id)
);

-- Create indexes for better performance
CREATE INDEX IX_Users_Username ON Users(username);
CREATE INDEX IX_Users_Email ON Users(email);
CREATE INDEX IX_Users_UserType ON Users(user_type);
CREATE INDEX IX_Wallets_UserId ON Wallets(user_id);
CREATE INDEX IX_Transactions_WalletId ON Transactions(wallet_id);
CREATE INDEX IX_Transactions_Date ON Transactions(transaction_date);
CREATE INDEX IX_WalletAdjustments_WalletId ON WalletAdjustments(wallet_id);

-- Insert default categories
INSERT INTO Categories (category_name, category_type, is_system_category) VALUES
('Salary', 'Income', 1),
('Business', 'Income', 1),
('Investment', 'Income', 1),
('Gift', 'Income', 1),
('Other Income', 'Income', 1),
('Food & Dining', 'Expense', 1),
('Transportation', 'Expense', 1),
('Shopping', 'Expense', 1),
('Entertainment', 'Expense', 1),
('Bills & Utilities', 'Expense', 1),
('Healthcare', 'Expense', 1),
('Education', 'Expense', 1),
('Travel', 'Expense', 1),
('Groceries', 'Expense', 1),
('Other Expense', 'Expense', 1);

-- Create admin user
INSERT INTO Users (username, email, password, user_type, display_name) VALUES
('admin', 'admin@thriftify.com', 'C7AD44CBAD762A5DA0A452F9E854FDC1E0E7A52A38015F23F3EAB1D80B931DD472634DFAC71CD34EBC35D16AB7FB8A90C81F975113D6C7538DC69DD8DE9077EC', 'Admin', 'System Administrator');

-- Create database administrator user
INSERT INTO Users (username, email, password, user_type, display_name) VALUES
('data_admin', 'data.admin@thriftify.com', 'C7AD44CBAD762A5DA0A452F9E854FDC1E0E7A52A38015F23F3EAB1D80B931DD472634DFAC71CD34EBC35D16AB7FB8A90C81F975113D6C7538DC69DD8DE9077EC', 'Database Administrator', 'Database Administrator');

-- Create sample regular users
INSERT INTO Users (username, email, password, user_type, display_name) VALUES
('juan_dela_cruz', 'juan@example.com', 'C7AD44CBAD762A5DA0A452F9E854FDC1E0E7A52A38015F23F3EAB1D80B931DD472634DFAC71CD34EBC35D16AB7FB8A90C81F975113D6C7538DC69DD8DE9077EC', 'User', 'Juan Dela Cruz'),
('maria_santos', 'maria@example.com', 'C7AD44CBAD762A5DA0A452F9E854FDC1E0E7A52A38015F23F3EAB1D80B931DD472634DFAC71CD34EBC35D16AB7FB8A90C81F975113D6C7538DC69DD8DE9077EC', 'User', 'Maria Santos'),
('pedro_garcia', 'pedro@example.com', 'C7AD44CBAD762A5DA0A452F9E854FDC1E0E7A52A38015F23F3EAB1D80B931DD472634DFAC71CD34EBC35D16AB7FB8A90C81F975113D6C7538DC69DD8DE9077EC', 'User', 'Pedro Garcia');

-- Create sample wallets for user juan_dela_cruz (user_id = 3)
INSERT INTO Wallets (user_id, wallet_name, wallet_type, current_balance, initial_balance, background_color, text_color) VALUES
(3, 'Cash Wallet', 'Cash', 5000.00, 5000.00, '#8FD398', '#000000'),
(3, 'GCash Account', 'GCash', 2500.00, 2500.00, '#3B79BF', '#FFFFFF'),
(3, 'Maya Wallet', 'Maya', 1500.00, 1500.00, '#FF6B35', '#FFFFFF'),
(3, 'BPI Savings', 'Bank', 25000.00, 25000.00, '#1E3A8A', '#FFFFFF');

-- Create sample transactions
INSERT INTO Transactions (wallet_id, category_id, transaction_type, amount, description, transaction_date, balance_after_transaction) VALUES
(1, 1, 'Income', 15000.00, 'Monthly Salary', '2025-01-01 09:00:00', 20000.00),
(1, 6, 'Expense', 500.00, 'Jollibee Lunch', '2025-01-02 12:30:00', 19500.00),
(1, 7, 'Expense', 100.00, 'Jeepney Fare', '2025-01-02 18:00:00', 19400.00),
(2, 8, 'Expense', 1200.00, 'Online Shopping', '2025-01-03 14:20:00', 1300.00),
(2, 4, 'Income', 1000.00, 'Birthday Gift', '2025-01-04 10:00:00', 2300.00);

-- Create sample wallet adjustments
INSERT INTO WalletAdjustments (wallet_id, adjustment_type, adjustment_amount, description) VALUES
(1, 'Correction', 50.00, 'Found cash in pocket'),
(3, 'Income', 200.00, 'Cashback reward');

-- Create sample notifications
INSERT INTO Notifications (user_id, title, message, notification_type) VALUES
(3, 'Welcome to Thriftify!', 'Start tracking your expenses and managing your wallets effectively.', 'Success'),
(3, 'Budget Alert', 'You have spent 75% of your monthly food budget.', 'Warning'),
(3, 'New Feature', 'Dark mode is now available in Settings!', 'Info');

-- Create views for common queries
CREATE VIEW vw_UserWalletSummary AS
SELECT 
    u.user_id,
    u.username,
    u.display_name,
    COUNT(w.wallet_id) as total_wallets,
    SUM(w.current_balance) as total_balance,
    AVG(w.current_balance) as average_balance
FROM Users u
LEFT JOIN Wallets w ON u.user_id = w.user_id AND w.is_active = 1 AND w.is_deleted = 0
WHERE u.is_active = 1 AND u.is_deleted = 0
GROUP BY u.user_id, u.username, u.display_name;

CREATE VIEW vw_RecentTransactions AS
SELECT 
    t.transaction_id,
    u.username,
    w.wallet_name,
    c.category_name,
    t.transaction_type,
    t.amount,
    t.description,
    t.transaction_date,
    t.balance_after_transaction
FROM Transactions t
INNER JOIN Wallets w ON t.wallet_id = w.wallet_id
INNER JOIN Users u ON w.user_id = u.user_id
LEFT JOIN Categories c ON t.category_id = c.category_id
WHERE t.is_active = 1 AND t.is_deleted = 0
    AND w.is_active = 1 AND w.is_deleted = 0
    AND u.is_active = 1 AND u.is_deleted = 0;

-- Create stored procedures for common operations
GO
CREATE PROCEDURE sp_CreateUserWallet
    @user_id INT,
    @wallet_name NVARCHAR(100),
    @wallet_type NVARCHAR(50),
    @initial_balance DECIMAL(15,2),
    @background_color NVARCHAR(10),
    @text_color NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO Wallets (user_id, wallet_name, wallet_type, current_balance, initial_balance, background_color, text_color)
    VALUES (@user_id, @wallet_name, @wallet_type, @initial_balance, @initial_balance, @background_color, @text_color);
    
    SELECT SCOPE_IDENTITY() AS wallet_id;
END
GO

CREATE PROCEDURE sp_AddTransaction
    @wallet_id INT,
    @category_id INT,
    @transaction_type NVARCHAR(10),
    @amount DECIMAL(15,2),
    @description NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    DECLARE @current_balance DECIMAL(15,2);
    DECLARE @new_balance DECIMAL(15,2);
    
    -- Get current balance
    SELECT @current_balance = current_balance FROM Wallets WHERE wallet_id = @wallet_id;
    
    -- Calculate new balance
    IF @transaction_type = 'Income'
        SET @new_balance = @current_balance + @amount;
    ELSE
        SET @new_balance = @current_balance - @amount;
    
    -- Check if expense would make balance negative
    IF @new_balance < 0 AND @transaction_type = 'Expense'
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50001, 'Insufficient funds for this transaction', 1;
        RETURN;
    END
    
    -- Insert transaction
    INSERT INTO Transactions (wallet_id, category_id, transaction_type, amount, description, balance_after_transaction)
    VALUES (@wallet_id, @category_id, @transaction_type, @amount, @description, @new_balance);
    
    -- Update wallet balance
    UPDATE Wallets SET current_balance = @new_balance WHERE wallet_id = @wallet_id;
    
    COMMIT TRANSACTION;
    
    SELECT SCOPE_IDENTITY() AS transaction_id;
END
GO

CREATE PROCEDURE sp_SoftDeleteUser
    @user_id INT,
    @deleted_by INT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE Users 
    SET is_deleted = 1, updated_at = GETDATE()
    WHERE user_id = @user_id;
    
    -- Also soft delete user's wallets and transactions
    UPDATE Wallets 
    SET is_deleted = 1, updated_at = GETDATE()
    WHERE user_id = @user_id;
    
    UPDATE Transactions 
    SET is_deleted = 1, updated_at = GETDATE()
    WHERE wallet_id IN (SELECT wallet_id FROM Wallets WHERE user_id = @user_id);
END
GO

-- Grant permissions based on user types
-- Note: In a real application, these would be separate database users
-- For this demo, we'll use application-level security

PRINT 'Database setup completed successfully!';
PRINT 'Default admin credentials: admin / hello';
PRINT 'Default user credentials: juan_dela_cruz / hello';