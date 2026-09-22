using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace FastClose
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            var config = MainWindow.LoadConfig();
            TxtProcessName.Text = config.ProcessName;
            TxtWindowTitle.Text = config.WindowTitle;
            ChkMatchExactTitle.IsChecked = config.MatchExactTitle;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            var config = MainWindow.LoadConfig();
            config.ProcessName = TxtProcessName.Text.Trim();
            config.WindowTitle = TxtWindowTitle.Text.Trim();
            config.MatchExactTitle = ChkMatchExactTitle.IsChecked == true;
            MainWindow.SaveConfig(config);

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                // .NET 8 中需显式设置 UseShellExecute = true 才能调用系统默认浏览器打开 URL
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开链接失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}