using System.Windows;

namespace FINALSTHRIFTIFY
{
    public partial class InputDialog : Window
    {
        public string ResponseText { get; set; }
        public string Title { get; set; }
        public string Prompt { get; set; }

        public InputDialog(string title, string prompt)
        {
            InitializeComponent();
            Title = title;
            Prompt = prompt;
            DataContext = this;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = InputTextBox.Text;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}