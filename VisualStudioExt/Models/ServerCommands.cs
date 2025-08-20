using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyCompressor;
using Nerdbank.Streams;
using VisualStudioExt.Pages;

namespace VisualStudioExt.Models
{
    internal class CommandTcpListener : TcpListener
    {
        public bool IsAcrive => this.Active;

        public CommandTcpListener(IPAddress localaddr, int port)
            : base(localaddr, port) { }

        public static new CommandTcpListener Create(int port)
        {
            var tcpListener = new CommandTcpListener(IPAddress.IPv6Any, port);
            tcpListener.Server.DualMode = true;
            return tcpListener;
        }
    }

    internal static class ServerCommands
    {
        private static CommandTcpListener server;
        private static Stream output;
        private static SnappierCompressor compressor;
        private static readonly CancellationTokenSource cancelTokenSource =
            new CancellationTokenSource();

        public static void Start()
        {
            if (server?.IsAcrive is true)
            {
                return;
            }

            try
            {
                _ = Task.Run(async () =>
                {
                    server = CommandTcpListener.Create(8883);
                    server.Start();
                    _ = DebugWindowControl.AddLoggingAsync($"服务端监听已开启");
                    while (!cancelTokenSource.Token.IsCancellationRequested)
                    {
                        var client = await server.AcceptTcpClientAsync();
                        Connect(client, cancelTokenSource.Token);
                    }
                });
            }
            catch { }
        }

        private static void Connect(TcpClient client, CancellationToken cancelToken)
        {
            _ = Task.Run(
                async () =>
                {
                    using var stream = client.GetStream();

                    var options = new MultiplexingStream.Options
                    {
                        DefaultChannelReceivingWindowSize = 1024 * 1024 * 8,
                        ProtocolMajorVersion = 3,
                    };
                    var multiplexor = await MultiplexingStream.CreateAsync(stream, options);

                    var inputChannel = await multiplexor.AcceptChannelAsync("input");
                    var outputChannel = await multiplexor.AcceptChannelAsync("output");

                    using var input = inputChannel.AsStream();
                    output = outputChannel.AsStream();

                    var initPayload = new DebugPayload { Type = DebugType.Init };
                    var initData = initPayload.ToBytes();
                    await output.WriteAsync(initData, 0, initData.Length);

                    while (!cancelToken.IsCancellationRequested)
                    {
                        var length = (await input.ReadBlockAsync(4, cancelToken)).ToInt32();
                        var payload = (
                            await input.ReadBlockAsync(length, cancelToken)
                        ).ToDebugPayload();

                        switch (payload.Type)
                        {
                            case DebugType.Init:
                            {
                                var info = Encoding.UTF8.GetString(payload.Body);
                                _ = DebugWindowControl.AddLoggingAsync($"连接设备成功: {info} ");
                                break;
                            }
                            case DebugType.Logging:
                            {
                                _ = DebugWindowControl.AddLoggingAsync(
                                    Encoding.UTF8.GetString(payload.Body)
                                );
                                break;
                            }
                            case DebugType.RunProject:
                            {
                                _ = RunProjectAsync();
                                break;
                            }
                            case DebugType.SaveProject:
                            {
                                _ = SaveProjectAsync();
                                break;
                            }
                            case DebugType.ScreenShot:
                                break;
                        }
                    }
                    output.Dispose();
                    output = null;

                    inputChannel.Dispose();
                    outputChannel.Dispose();
                },
                cancelToken
            );
        }

        public static bool CheckIsConnected()
        {
            return output != null;
        }

        private static async Task SendProjectAsync(DebugType type)
        {
            var rootDirBase = await DebugWindowControl.GetProjectDirAsync();
            var rootDir = $"{rootDirBase}{Path.DirectorySeparatorChar}";
            var ignoreDirs = new[]
            {
                Path.Combine(rootDir, ".vs"),
                Path.Combine(rootDir, ".vscode"),
                Path.Combine(rootDir, ".git"),
                Path.Combine(rootDir, "bin"),
                Path.Combine(rootDir, "obj"),
            };

            await Task.Run(async () =>
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

                    var files = Directory.GetFiles(
                        dir,
                        "*",
                        dir == rootDir ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories
                    );
                    foreach (var f in files)
                    {
                        var relativePath = f.Replace(rootDir, default)
                            .Replace(Path.DirectorySeparatorChar, '/');
                        var entry = zip.CreateEntry(relativePath);
                        using var stream = entry.Open();
                        using var fs = new FileStream(f, FileMode.Open, FileAccess.Read);
                        await fs.CopyToAsync(stream);
                    }
                }

                zip.Dispose();
                var zipBytes = ms.ToArray();
                var directories = rootDir.Split(Path.DirectorySeparatorChar);
                var id = Encoding.UTF8.GetBytes(directories[directories.Length - 2]);

                compressor ??= new SnappierCompressor();
                var compressData = compressor.Compress(zipBytes);

                var bodyStream = new MemoryStream(4 + id.Length + compressData.Length);
                bodyStream.WriteInt32(id.Length);
                await bodyStream.WriteAsync(id, 0, id.Length);
                await bodyStream.WriteAsync(compressData, 0, compressData.Length);
                var bytes = bodyStream.ToArray();

                var payload = new DebugPayload { Type = type, Body = bytes };
                var data = payload.ToBytes();
                await output.WriteAsync(data, 0, data.Length);
            });
        }

        public static async Task RunProjectAsync()
        {
            await SendProjectAsync(DebugType.RunProject);
        }

        public static async Task SaveProjectAsync()
        {
            await SendProjectAsync(DebugType.SaveProject);
        }
    }
}
