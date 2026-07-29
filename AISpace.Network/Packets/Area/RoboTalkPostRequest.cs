namespace AISpace.Network.Packets.Area;

/// <summary>
/// A message produced by the client-side script for a Robo conversation.
/// Payload: UInt RoboId + null-terminated UTF-8 Message.
/// </summary>
public sealed class RoboTalkPostRequest : IIncomingPacket<RoboTalkPostRequest>
{
    public uint RoboId { get; init; }
    public string Message { get; init; } = string.Empty;

    public static RoboTalkPostRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new RoboTalkPostRequest
        {
            RoboId = reader.ReadUInt(),
            Message = reader.ReadString("utf-8"),
        };
    }
}
