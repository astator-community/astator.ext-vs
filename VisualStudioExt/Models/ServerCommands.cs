using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Server;
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
    internal static class ServerCommands
    {
        private static IMqttServer server;
        private static int ClientCount;

        public static async Task StartAsync()
        {
            try
            {
                var mqttFactory = new MqttFactory();
                var mqttServerOptions = new MqttServerOptionsBuilder()
                    .Build();

                server = mqttFactory.CreateMqttServer();

                server.UseClientConnectedHandler(async (e) =>
                {
                    ClientCount++;
                    await server.SubscribeAsync(e.ClientId,
                        new MqttTopicFilter
                        {
                            Topic = "server/init"
                        },
                        new MqttTopicFilter
                        {
                            Topic = "server/logging"
                        });
                });

                server.UseClientDisconnectedHandler(async (e) =>
                {
                    ClientCount--;
                    await server.UnsubscribeAsync(e.ClientId,
                        "server/run-init",
                        "server/run-logging");
                });

                server.UseApplicationMessageReceivedHandler(ApplicationMessageReceived);

                await server.StartAsync(mqttServerOptions);
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
            if (ClientCount <= 0 || !(server?.IsStarted ?? false))
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
                _ = server.PublishAsync(new MqttApplicationMessage
                {
                    Topic = topic,
                    Payload = packData
                });
            });
        }

        public static async Task RunProjectAsync()
        {
            if (!CheckIsConnected()) return;

            await SendProjectAsync("client/run-project");
        }

        public static async Task RunScriptAsync()
        {
            if (!CheckIsConnected()) return;

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
            if (!CheckIsConnected()) return;

            await SendProjectAsync("client/save-project");
        }
    }
}
