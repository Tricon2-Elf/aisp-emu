using AISpace.Common.Game;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class AreaMyProfileCloseHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.MyProfileCloseRequest;
    public PacketType ResponseType => (PacketType)0;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        await Task.CompletedTask;
    }
}
