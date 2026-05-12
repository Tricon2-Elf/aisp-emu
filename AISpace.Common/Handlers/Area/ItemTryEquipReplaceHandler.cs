using AISpace.Common.Game;
using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class ItemTryEquipReplaceHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.ItemTryEquipReplaceRequest;
    public PacketType ResponseType => PacketType.ItemTryEquipped;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        // 1. Confirm equipment (0xBB7C)
        var writer = new PacketWriter();
        writer.Write(session.CharacterId);
        writer.Write((uint)0);
        writer.Write((uint)0);
        await session.SendAsync(PacketType.ItemTryEquipped, writer.ToBytes(), ct);

        // 2. Send signal to close window (0xB4A8)
        var endWriter = new PacketWriter();
        endWriter.Write(session.CharacterId);
        await session.SendAsync(PacketType.ItemEquipEnded, endWriter.ToBytes(), ct);
    }
}
