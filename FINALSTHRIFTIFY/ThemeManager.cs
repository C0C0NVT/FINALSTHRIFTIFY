using System;
using System.ComponentModel;
using System.Windows.Media;

namespace FINALSTHRIFTIFY
{
    public class ThemeManager : INotifyPropertyChanged
    {
        private static ThemeManager _instance;
        private bool _isDarkTheme = false;

        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ThemeManager();
                return _instance;
            }
        }

        private ThemeManager() { }

        public bool IsDarkTheme
        {
            get { return _isDarkTheme; }
            set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;
                    OnPropertyChanged(nameof(IsDarkTheme));
                    OnPropertyChanged(nameof(BackgroundColor));
                    OnPropertyChanged(nameof(TextColor));
                    OnPropertyChanged(nameof(CardBackground));
                    OnPropertyChanged(nameof(SecondaryBackground));
                }
            }
        }

        public Color BackgroundColor
        {
            get { return _isDarkTheme ? Color.FromRgb(44, 44, 44) : Color.FromRgb(95, 159, 123); }
        }

        public Color TextColor
        {
            get { return _isDarkTheme ? Color.FromRgb(143, 211, 152) : Colors.White; }
        }

        public Color CardBackground
        {
            get { return _isDarkTheme ? Color.FromRgb(60, 60, 60) : Color.FromRgb(245, 245, 245); }
        }

        public Color SecondaryBackground
        {
            get { return _isDarkTheme ? Color.FromRgb(50, 50, 50) : Color.FromRgb(105, 136, 109); }
        }

        public Color BorderColor
        {
            get { return _isDarkTheme ? Color.FromRgb(80, 80, 80) : Color.FromRgb(224, 224, 224); }
        }

        public Color AccentColor
        {
            get { return Color.FromRgb(69, 161, 118); } // Always green for accent
        }

        public Color SuccessColor
        {
            get { return Color.FromRgb(143, 211, 152); }
        }

        public Color ErrorColor
        {
            get { return Color.FromRgb(159, 9, 9); }
        }

        public Color WarningColor
        {
            get { return Color.FromRgb(255, 193, 7); }
        }

        public void ToggleTheme()
        {
            IsDarkTheme = !IsDarkTheme;
        }

        public void SetTheme(bool isDark)
        {
            IsDarkTheme = isDark;
        }

        public void ApplyThemeToWindow(System.Windows.Window window)
        {
            if (window == null) return;

            try
            {
                // Update window background
                window.Background = new SolidColorBrush(BackgroundColor);

                // Find and update dynamic resources if they exist
                if (window.Resources.Contains("DynamicBackground"))
                {
                    window.Resources["DynamicBackground"] = new SolidColorBrush(BackgroundColor);
                }
                if (window.Resources.Contains("DynamicText"))
                {
                    window.Resources["DynamicText"] = new SolidColorBrush(TextColor);
                }
                if (window.Resources.Contains("DynamicCard"))
                {
                    window.Resources["DynamicCard"] = new SolidColorBrush(CardBackground);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Extension methods for easier theme application
    public static class ThemeExtensions
    {
        public static void ApplyCurrentTheme(this System.Windows.Window window)
        {
            ThemeManager.Instance.ApplyThemeToWindow(window);
        }

        public static SolidColorBrush GetThemedBrush(this Color color)
        {
            return new SolidColorBrush(color);
        }

        public static void UpdateThemeColors(this System.Windows.FrameworkElement element)
        {
            if (element == null) return;

            var themeManager = ThemeManager.Instance;
            
            // Update background if it's using dynamic theme
            if (element.GetValue(System.Windows.Controls.Panel.BackgroundProperty) is SolidColorBrush bgBrush)
            {
                if (bgBrush.Color == Color.FromRgb(95, 159, 123) || bgBrush.Color == Color.FromRgb(44, 44, 44))
                {
                    element.SetValue(System.Windows.Controls.Panel.BackgroundProperty, 
                        new SolidColorBrush(themeManager.BackgroundColor));
                }
            }
        }
    }
}