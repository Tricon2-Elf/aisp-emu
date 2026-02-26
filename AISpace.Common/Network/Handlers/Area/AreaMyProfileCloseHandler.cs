namespace AISpace.Common.Network.Handlers;

public class AreaMyProfileCloseHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.MyProfileCloseRequest;
    public PacketType ResponseType => (PacketType)0; 
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        await Task.CompletedTask;
    }
}