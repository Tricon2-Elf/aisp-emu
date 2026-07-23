namespace AISpace.Network.Data;

/// <summary>
/// Avatar or Robo status text and its selected status icon. This is also the
/// payload used by <c>recv_notify_user_status_update</c>.
/// </summary>
public sealed class UserStatusData
{
    public const int StatusTextLength = 49;
    public const int WireSize = StatusTextLength + sizeof(uint);

    public string StatusText { get; set; } = string.Empty;
    public uint StatusIconId { get; set; }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.WriteFixedString(StatusText, StatusTextLength, "utf-8");
        writer.Write(StatusIconId);
        return writer.ToBytes();
    }

    public static UserStatusData FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < WireSize)
            throw new ArgumentException($"UserStatusData requires at least {WireSize} bytes.", nameof(data));

        var reader = new PacketReader(data);
        return new UserStatusData { StatusText = reader.ReadFixedString(StatusTextLength, "utf-8"), StatusIconId = reader.ReadUInt() };
    }
}
