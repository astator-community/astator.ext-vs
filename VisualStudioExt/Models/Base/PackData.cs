using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nerdbank.Streams;

namespace VisualStudioExt.Models.Base { }

public struct PackPayload
{
    public byte[] Header { get; set; }
    public byte[] Body { get; set; }
}

internal enum DebugType
{
    Init = 1,
    Logging = 10,
    RunProject = 11,
    SaveProject = 100,
    ScreenShot = 101,
    heartBeat = 110,
}

internal struct DebugPayload
{
    public DebugType Type { get; set; }
    public byte[] Body { get; set; }
}

public static class PackDataExtension
{
    public static byte[] ToBytes(this PackPayload payload)
    {
        var size = 8 + (payload.Body?.Length ?? 0);
        using var ms = new MemoryStream(size + 4);
        ms.WriteInt32(size);

        ms.Write(payload.Header, 0, 8);

        if (payload.Body != null)
        {
            ms.Position = 4 + 8;
            ms.Write(payload.Body, 0, payload.Body.Length);
        }

        return ms.GetBuffer();
    }

    public static PackPayload ToPackPayload(this byte[] bytes)
    {
        var result = new PackPayload();

        var ms = bytes.AsMemory();
        result.Header = ms.Slice(0, 8).ToArray();
        result.Body = ms.Slice(8).ToArray();

        return result;
    }

    internal static byte[] ToBytes(this DebugPayload payload)
    {
        var size = 8 + (payload.Body?.Length ?? 0);
        using var ms = new MemoryStream(size + 4);
        ms.WriteInt32(size);

        ms.WriteInt32((int)payload.Type);

        if (payload.Body != null)
        {
            ms.Position = 4 + 8;
            ms.Write(payload.Body, 0, payload.Body.Length);
        }

        return ms.GetBuffer();
    }

    internal static DebugPayload ToDebugPayload(this byte[] bytes)
    {
        var result = new DebugPayload();

        var ms = bytes.AsMemory();
        result.Type = (DebugType)ms.Slice(0, 4).ToArray().ToInt32();
        result.Body = ms.Slice(8).ToArray();

        return result;
    }

    internal static byte[] ToHeader(this DebugType type)
    {
        var i32 = (int)type;
        var bytes = new byte[8];
        bytes[0] = (byte)(i32 >> 24);
        bytes[1] = (byte)(i32 >> 16);
        bytes[2] = (byte)(i32 >> 8);
        bytes[3] = (byte)i32;
        return bytes;
    }

    public static byte[] ToBytes(this int i32)
    {
        var bytes = new byte[4];
        bytes[0] = (byte)(i32 >> 24);
        bytes[1] = (byte)(i32 >> 16);
        bytes[2] = (byte)(i32 >> 8);
        bytes[3] = (byte)i32;
        return bytes;
    }

    public static int ToInt32(this byte[] value, int offset = 0)
    {
        int result;
        result = value[offset] << 24 | value[offset + 1] << 16 | value[offset + 2] << 8 | value[offset + 3];
        return result;
    }

    public static void WriteInt32(this Stream stream, int i32)
    {
        stream.Write(i32.ToBytes(), 0, 4);
    }

    public static int ReadInt32(this Stream stream)
    {
        var bytes = new byte[4];
        stream.Read(bytes, 0, 4);
        return bytes.ToInt32();
    }

    public static async Task<byte[]> ReadBlockAsync(
        this Stream stream,
        int length,
        CancellationToken cancelToken = default
    )
    {
        var result = new byte[length];
        var memory = result.AsMemory();

        var offset = 0;
        while (offset < length)
            offset += await stream.ReadBlockAsync(memory.Slice(offset), cancelToken);

        return result;
    }
}
