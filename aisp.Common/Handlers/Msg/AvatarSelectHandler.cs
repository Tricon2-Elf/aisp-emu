using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class AvatarSelectHandler(SharedState state) : IPacketHandler, IRequiresAuthenticatedSession
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
        var request = AvatarSelectRequest.FromBytes(payload.Span);
        var characters = session.User!.Characters.OrderBy(c => c.Id).ToList();
        if (request.SlotId < characters.Count)
        {
            var character = characters[(int)request.SlotId];
            session.CharacterId = (uint)character.Id;
            session.Character = character;
            // Refresh presence so circle online lookups see the selected CharacterId.
            state.RegisterClient(ServerType.Msg, session);
        }

        await session.SendAsync(ResponseType, new AvatarSelectResponse(0).ToBytes(), ct);
    }
}
