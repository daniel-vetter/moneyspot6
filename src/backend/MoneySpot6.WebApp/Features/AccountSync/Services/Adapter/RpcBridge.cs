using System.Text;
using System.Text.Json;

namespace MoneySpot6.WebApp.Features.AccountSync.Services.Adapter;

public class RpcBridge(Stream inputStream, Stream outputStream)
{
    private readonly Dictionary<string, Type> _registeredMessageTypes = new();

    public async Task<RpcBridge> Connect(CancellationToken ct)
    {
        await WriteInt(1, ct);
        return this;
    }

    public async Task<int> ReadInt(CancellationToken ct)
    {
        return BitConverter.ToInt32(await ReadBytes(4, ct));
    }

    public async Task<string> ReadString(CancellationToken ct)
    {
        var len = await ReadInt(ct);
        var buffer = new byte[len];
        await inputStream.ReadExactlyAsync(buffer, 0, len, ct);
        return Encoding.UTF8.GetString(buffer);
    }

    public async Task<byte[]> ReadBytes(int count, CancellationToken ct)
    {
        var buffer = new byte[count];
        await inputStream.ReadExactlyAsync(buffer, 0, count, ct);
        return buffer;
    }

    public async Task WriteInt(int value, CancellationToken ct)
    {
        var bytes = BitConverter.GetBytes(value);
        await outputStream.WriteAsync(bytes, ct);
        await outputStream.FlushAsync(ct);
    }

    public async Task WriteString(string value, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        await WriteInt(bytes.Length, ct);
        await outputStream.WriteAsync(bytes, ct);
        await outputStream.FlushAsync(ct);
    }

    public async Task Send<T>(T obj, CancellationToken ct)
    {
        await WriteString(typeof(T).Name, ct);
        await WriteString(JsonSerializer.Serialize(obj), ct);
    }

    public async Task<object> Read(CancellationToken ct)
    {
        var messageType = await ReadString(ct);
        if (!_registeredMessageTypes.TryGetValue(messageType, out var type))
            throw new Exception($"No message type for message '{messageType}' registered.");

        var json = await ReadString(ct);
        var obj = JsonSerializer.Deserialize(json, type);

        return obj ?? throw new Exception($"Deserialization of message {messageType}'' returned null.");
    }

    public RpcBridge RegisterIncomingMessageType<T>()
    {
        _registeredMessageTypes.Add(typeof(T).Name, typeof(T));
        return this;
    }
}