using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VisualStudioExt.Models.Base;
using VisualStudioExt.Pages;

namespace VisualStudioExt.Models
{
    public static class ClientCommands
    {
        private static IMqttClient client;
        private static bool isConnected;

        public static async Task ConnectAsync(string ip)
        {
            try
            {
                client?.Dispose();
                var mqttClientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer((op) =>
                    {
                        op.Server = ip;
                        op.BufferSize = 1024 * 1024 * 10;
                    })
                    .WithKeepAlivePeriod(TimeSpan.FromMinutes(6))
                    .Build();

                var mqttFactory = new MqttFactory();
                client = mqttFactory.CreateMqttClient();

                client.UseApplicationMessageReceivedHandler(ApplicationMessageReceived);
                client.UseConnectedHandler(async (e) =>
                {
                    isConnected = true;
                    await client.SubscribeAsync(
                        new MqttTopicFilter
                        {
                            Topic = "server/init"
                        },
                        new MqttTopicFilter
                        {
                            Topic = "server/logging"
                        });

                    await client.PublishAsync("client/init");
                });
                client.UseDisconnectedHandler(async (e) =>
                {
                    DebugWindowControl.AddLogging("断开连接");
                    await client.UnsubscribeAsync("server/init", "server/logging");
                    client.Dispose();
                    isConnected = false;
                });

                await client.ConnectAsync(mqttClientOptions, CancellationToken.None);
            }
            catch (Exception ex)
            {
                DebugWindowControl.AddLogging(ex.Message);
            }
        }

        private static void ApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            var pack = PackData.Parse(e.ApplicationMessage.Payload);
            if (pack is null) return;

            switch (e.ApplicationMessage.Topic)
            {
                case "server/init":
                    DebugWindowControl.AddLogging($"连接设备成功: {pack.Key} {pack.Description}");
                    break;
                case "server/logging":
                    DebugWindowControl.AddLogging($"{pack.Key} {Encoding.UTF8.GetString(pack.Buffer)}");
                    break;
            }
        }

        public static bool CheckIsConnected()
        {
            if (!isConnected || !(client?.IsConnected ?? false))
                return false;
            else
                return true;
        }

        private static async Task SendProjectAsync(string topic, string desc = default)
        {
            var rootDir = $"{VisualStudioExtPackage.GetProjectDir()}{Path.DirectorySeparatorChar}";
            var ignoreDirs = new[]
            {
                Path.Combine(rootDir, ".vs"),
                Path.Combine(rootDir, ".vscode"),
                Path.Combine(rootDir, ".git"),
                Path.Combine(rootDir, "bin"),
                Path.Combine(rootDir, "obj"),
            };

            await Task.Run(() =>
            {
                using var ms = new MemoryStream();
                var zip = new ZipArchive(ms, ZipArchiveMode.Create);

                var dirs = Directory.GetDirectories(rootDir).ToList();
                dirs.Add(rootDir);

                foreach (var dir in dirs)
                {
                    if (ignoreDirs.Contains(dir))
                    {
                        continue;
                    }

                    var files = Directory.GetFiles(dir, "*", dir == rootDir ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
                    foreach (var f in files)
                    {
                        var relativePath = f.Replace(rootDir, default).Replace(Path.DirectorySeparatorChar, '/');
                        var entry = zip.CreateEntry(relativePath);
                        using var stream = entry.Open();
                        using var fs = new FileStream(f, FileMode.Open, FileAccess.Read);
                        fs.CopyTo(stream);
                    }
                }

                zip.Dispose();
                var bytes = ms.ToArray();
                var directories = rootDir.Split(Path.DirectorySeparatorChar);
                var id = directories[directories.Length - 2];
                var packData = Stick.MakePackData(id, desc ?? string.Empty, bytes);
                _ = client.PublishAsync(topic, packData);
            });
        }

        public static async Task RunProjectAsync()
        {
            await SendProjectAsync("client/run-project");
        }

        public static async Task RunScriptAsync()
        {
            var scriptPathName = VisualStudioExtPackage.GetActiveDocumentName();

            if (scriptPathName == null)
            {
                MessageBox.Show("未找到已激活文档窗口!");
                return;
            };

            await SendProjectAsync("client/run-script", scriptPathName);
        }

        public static async Task SaveProjectAsync()
        {
            await SendProjectAsync("client/save-project");
        }
    }
}
