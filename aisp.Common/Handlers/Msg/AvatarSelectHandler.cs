using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class AvatarSelectHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AvatarSelectRequest;
    public PacketType ResponseType => PacketType.AvatarSelectResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var cha = session.User!.Characters.FirstOrDefault();

        var response = new AvatarSelectResponse(0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
