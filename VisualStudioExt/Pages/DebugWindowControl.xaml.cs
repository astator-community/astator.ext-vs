using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using VisualStudioExt.Models;

namespace VisualStudioExt.Pages
{
    /// <summary>
    /// Interaction logic for DebugWindowControl.
    /// </summary>
    public partial class DebugWindowControl : UserControl
    {
        private readonly string extDir;
        private readonly string historyDevicesPath;

        private static DebugWindowControl instance;

        public DebugWindowControl()
        {
            instance = this;
            this.InitializeComponent();
            this.extDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "astator-VisualStudioExt"
            );
            this.historyDevicesPath = Path.Combine(this.extDir, "historyDevices.txt");

            if (File.Exists(this.historyDevicesPath))
            {
                var lines = File.ReadLines(this.historyDevicesPath);
                this.ConnectLatestDevice.ToolTip = $"连接到最近的设备: {lines.ElementAt(0)}";
            }
            else
            {
                this.ConnectLatestDevice.ToolTip = $"连接到最近的设备";
            }
        }

        private void StartServiceListener_Clicked(object sender, RoutedEventArgs e)
        {
            ServerCommands.Start();
        }

        public static async Task AddLoggingAsync(string msg)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            instance.LogText.Text += $"{msg}\r\n";
        }

        private void LogText_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.Scroller.ScrollToBottom();
        }

        private void RunProject_Clicked(object sender, RoutedEventArgs e)
        {
            if (!ClientCommands.CheckIsConnected() && !ServerCommands.CheckIsConnected())
            {
                MessageBox.Show("未连接设备, 请连接后重试!");
            }
            if (ClientCommands.CheckIsConnected())
                _ = ClientCommands.RunProjectAsync();
            if (ServerCommands.CheckIsConnected())
                _ = ServerCommands.RunProjectAsync();
        }

        private void SaveProject_Clicked(object sender, RoutedEventArgs e)
        {
            if (!ClientCommands.CheckIsConnected() && !ServerCommands.CheckIsConnected())
            {
                MessageBox.Show("未连接设备, 请连接后重试!");
            }
            if (ClientCommands.CheckIsConnected())
                _ = ClientCommands.SaveProjectAsync();
            if (ServerCommands.CheckIsConnected())
                _ = ServerCommands.SaveProjectAsync();
        }

        private void Connect_Clicked(object sender, RoutedEventArgs e)
        {
            var historyDevices = new List<string>();

            if (File.Exists(this.historyDevicesPath))
            {
                var list = File.ReadAllLines(this.historyDevicesPath).ToList();
                historyDevices = list.GetRange(0, list.Count > 10 ? 10 : list.Count);
            }

            var connect = new Connect(historyDevices);
            if (connect.ShowDialog() == true)
            {
                var ip = Connect.LatestIp;
                if (!string.IsNullOrEmpty(ip))
                {
                    if (!Directory.Exists(this.extDir))
                        Directory.CreateDirectory(this.extDir);
                    if (historyDevices.Contains(ip))
                        historyDevices.Remove(ip);
                    historyDevices.Insert(0, ip);
                    File.WriteAllLines(this.historyDevicesPath, historyDevices);
                    this.ConnectLatestDevice.ToolTip = $"连接到最近的设备: {ip}";

                    ClientCommands.Connect(ip);
                }
            }
        }

        private void ConnectLatestDevice_Clicked(object sender, RoutedEventArgs e)
        {
            if (File.Exists(this.historyDevicesPath))
            {
                var lines = File.ReadLines(this.historyDevicesPath);
                ClientCommands.Connect(lines.ElementAt(0));
            }
        }

        private void ClearLogging_Clicked(object sender, RoutedEventArgs e)
        {
            this.LogText.Clear();
        }

        internal static async Task<string> GetProjectDirAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            return VisualStudioExtPackage.GetProjectDir();
        }
    }
}
