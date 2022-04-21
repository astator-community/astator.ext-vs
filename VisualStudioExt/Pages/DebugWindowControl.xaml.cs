using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
            TryConnectLatestHistoryDevice();
            if (!ClientCommands.CheckIsConnected() && !ServerCommands.CheckIsConnected())
            {
                MessageBox.Show("未连接设备, 请连接后重试!");
            }
            if (ClientCommands.CheckIsConnected()) _ = ClientCommands.RunProjectAsync();
            if (ServerCommands.CheckIsConnected()) _ = ServerCommands.RunProjectAsync();
        }

        private void RunScript_Clicked(object sender, RoutedEventArgs e)
        {
            TryConnectLatestHistoryDevice();
            if (!ClientCommands.CheckIsConnected() && !ServerCommands.CheckIsConnected())
            {
                MessageBox.Show("未连接设备, 请连接后重试!");
            }
            if (ClientCommands.CheckIsConnected()) _ = ClientCommands.RunScriptAsync();
            if (ServerCommands.CheckIsConnected()) _ = ServerCommands.RunScriptAsync();
        }

        private void SaveProject_Clicked(object sender, RoutedEventArgs e)
        {
            TryConnectLatestHistoryDevice();
            if (!ClientCommands.CheckIsConnected() && !ServerCommands.CheckIsConnected())
            {
                MessageBox.Show("未连接设备, 请连接后重试!");
            }
            if (ClientCommands.CheckIsConnected()) _ = ClientCommands.SaveProjectAsync();
            if (ServerCommands.CheckIsConnected()) _ = ServerCommands.SaveProjectAsync();
        }

        private void Connect_Clicked(object sender, RoutedEventArgs e)
        {
            List<string> historyDevices = GetHistoryDevices();
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

        /// <summary>
        /// 尝试连接最后一次链接的设备
        /// </summary>
        /// <param name="waittingMillis"></param>
        /// <returns></returns>
        private bool TryConnectLatestHistoryDevice(int waittingMillis = 3000)
        {
            if (ClientCommands.CheckIsConnected() || ServerCommands.CheckIsConnected())
            {
                return true;
            }
            List<string> historyDevices = GetHistoryDevices();

            if (historyDevices == null || historyDevices.Count < 0)
            {
                return false;
            }
            var ip = historyDevices.FirstOrDefault();
            if (string.IsNullOrEmpty(ip))
            {
                return false;
            }
            DebugWindowControl.AddLogging($"尝试连接到最近的设备: {ip}");
            Task.WaitAll(new[] { Models.ClientCommands.ConnectAsync(ip) }, waittingMillis);
            var connected = !ClientCommands.CheckIsConnected() && !ServerCommands.CheckIsConnected();
            DebugWindowControl.AddLogging($"链接到: {ip} {(connected ? "成功" : "失败")}");
            return connected;
        }

        private List<string> GetHistoryDevices()
        {
            var historyDevices = new List<string>();

            if (File.Exists(historyDevicesPath))
            {
                var list = File.ReadAllLines(historyDevicesPath).ToList();
                historyDevices = list.GetRange(0, list.Count > 10 ? 10 : list.Count);
            }

            return historyDevices;
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