using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VisualStudioExt.Pages
{
    /// <summary>
    /// Connect.xaml 的交互逻辑
    /// </summary>
    public partial class Connect : Window
    {
        public static string LatestIp = string.Empty;

        public Connect(List<string> historyDevices)
        {
            InitializeComponent();
            this.History.ItemsSource = historyDevices;
            this.Ip.Text = LatestIp;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.Ip.Text = this.History.SelectedItem as string;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.Ip.Text))
            {
                LatestIp = this.Ip.Text;
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("请先输入ip");
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!string.IsNullOrEmpty(this.Ip.Text))
                {
                    LatestIp = this.Ip.Text;
                    this.DialogResult = true;
                }
                else
                {
                    MessageBox.Show("请先输入ip");
                }
            }
        }
    }
}
