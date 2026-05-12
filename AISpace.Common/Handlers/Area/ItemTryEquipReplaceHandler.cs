using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class ItemTryEquipReplaceHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.ItemTryEquipReplaceRequest;
    public PacketType ResponseType => PacketType.ItemTryEquipped;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        await session.SendAsync(PacketType.ItemTryEquipped, new ItemTryEquipped(session.CharacterId, 0, 0).ToBytes(), ct);
        await session.SendAsync(PacketType.ItemEquipEnded, new ItemEquipEnded(session.CharacterId).ToBytes(), ct);
    }
}
