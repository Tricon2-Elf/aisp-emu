using AISpace.Network.Packets.Msg;
using AISpace.Network;

using AISpace.Common.Game;

namespace AISpace.Common.Handlers.Msg;

public class AvatarSelectHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.AvatarSelectRequest;
    public PacketType ResponseType => PacketType.AvatarSelectResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        if (session.User == null)
            return;
        var cha = session.User.Characters.FirstOrDefault();

        var response = new AvatarSelectResponse(0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
