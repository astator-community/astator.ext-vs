using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
            extDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "astator-VisualStudioExt");
            historyDevicesPath = Path.Combine(extDir, "historyDevices.txt");

            if (File.Exists(historyDevicesPath))
            {
                var lines = File.ReadLines(historyDevicesPath);
                this.ConnectLatestDevice.ToolTip = $"连接到最近的设备: {lines.ElementAt(0)}";
            }
            else
            {
                this.ConnectLatestDevice.ToolTip = $"连接到最近的设备";
            }

            _ = ServerCommands.StartAsync();
        }

        public static void AddLogging(string msg)
        {
            instance.Dispatcher.Invoke(() =>
            {
                instance.LogText.Text += $"{msg}\r\n";
            });
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
            if (ClientCommands.CheckIsConnected()) _ = ClientCommands.RunProjectAsync();
            if (ServerCommands.CheckIsConnected()) _ = ServerCommands.RunProjectAsync();
        }

        private void RunScript_Clicked(object sender, RoutedEventArgs e)
        {
            if (!ClientCommands.CheckIsConnected() && !ServerCommands.CheckIsConnected())
            {
                MessageBox.Show("未连接设备, 请连接后重试!");
            }
            if (ClientCommands.CheckIsConnected()) _ = ClientCommands.RunScriptAsync();
            if (ServerCommands.CheckIsConnected()) _ = ServerCommands.RunScriptAsync();
        }

        private void SaveProject_Clicked(object sender, RoutedEventArgs e)
        {
            if (!ClientCommands.CheckIsConnected() && !ServerCommands.CheckIsConnected())
            {
                MessageBox.Show("未连接设备, 请连接后重试!");
            }
            if (ClientCommands.CheckIsConnected()) _ = ClientCommands.SaveProjectAsync();
            if (ServerCommands.CheckIsConnected()) _ = ServerCommands.SaveProjectAsync();
        }

        private void Connect_Clicked(object sender, RoutedEventArgs e)
        {
            var historyDevices = new List<string>();

            if (File.Exists(historyDevicesPath))
            {
                var list = File.ReadAllLines(historyDevicesPath).ToList();
                historyDevices = list.GetRange(0, list.Count > 10 ? 10 : list.Count);
            }

            var connect = new Connect(historyDevices);
            if (connect.ShowDialog() == true)
            {
                var ip = Connect.LatestIp;
                if (!string.IsNullOrEmpty(ip))
                {
                    if (!Directory.Exists(extDir)) Directory.CreateDirectory(extDir);
                    if (!historyDevices.Contains(ip)) historyDevices.Insert(0, ip);
                    File.WriteAllLines(historyDevicesPath, historyDevices);
                    this.ConnectLatestDevice.ToolTip = $"连接到最近的设备: {ip}";

                    _ = Models.ClientCommands.ConnectAsync(ip);
                }
            }
        }

        private void ConnectLatestDevice_Clicked(object sender, RoutedEventArgs e)
        {
            if (File.Exists(historyDevicesPath))
            {
                var lines = File.ReadLines(historyDevicesPath);
                _ = Models.ClientCommands.ConnectAsync(lines.ElementAt(0));
            }
        }

        private void ClearLogging_Clicked(object sender, RoutedEventArgs e)
        {
            this.LogText.Clear();
        }
    }
}