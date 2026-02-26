namespace AISpace.Common.Network.Handlers;

public enum MessageDomain
{
    Auth = 1,
    Area = 2,
    Msg = 3,
}

public interface IPacketHandler
{
    PacketType RequestType { get; }
    PacketType ResponseType { get; }
    MessageDomain Domain { get; }
    Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default);
}
