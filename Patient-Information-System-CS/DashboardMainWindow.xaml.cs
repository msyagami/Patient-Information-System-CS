using Patient_Information_System_CS.ViewModels;
using Patient_Information_System_CS.Models;
using System.Windows;
using System.Windows.Controls;

namespace Patient_Information_System_CS
{
    public partial class DashboardMainWindow : Window
    {
        private MainViewModel _viewModel;

        public DashboardMainWindow(UserAccount account)
        {
            InitializeComponent();

            _viewModel = new MainViewModel(account);
            DataContext = _viewModel;
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton button && button.Tag is string viewName)
            {
                _viewModel.SelectedNavigationItem = viewName;
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Show login window and close dashboard
            var loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}
