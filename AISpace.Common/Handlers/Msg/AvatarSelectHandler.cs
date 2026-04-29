using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public class AvatarSelectHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.AvatarSelectRequest;
    public PacketType ResponseType => PacketType.AvatarSelectResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        if (session.User == null)
            return;
        var cha = session.User.Characters.FirstOrDefault();

        var response = new AvatarSelectResponse(0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
