using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class ItemTryEquipReplaceHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.ItemTryEquipReplaceRequest;
    public PacketType ResponseType => PacketType.ItemTryEquipped;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        // 1. Confirm equipment (0xBB7C)
        var writer = new PacketWriter();
        writer.Write(connection.CharacterId);
        writer.Write((uint)0);
        writer.Write((uint)0);
        await connection.SendAsync(PacketType.ItemTryEquipped, writer.ToBytes(), ct);

        // 2. Send signal to close window (0xB4A8)
        var endWriter = new PacketWriter();
        endWriter.Write(connection.CharacterId);
        await connection.SendAsync(PacketType.ItemEquipEnded, endWriter.ToBytes(), ct);
    }
}
