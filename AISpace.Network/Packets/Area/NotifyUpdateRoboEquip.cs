using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// Updates the fixed equipment array for the Robo identified by both its owner-local Robo ID and runtime object ID.
/// </summary>
public sealed class NotifyUpdateRoboEquip(uint roboId, uint objectId, IEnumerable<ItemEquipEntry> equipment) : IOutgoingPacket
{
    public const int MaximumEquipmentCount = 30;

    public uint RoboId { get; } = roboId;
    public uint ObjectId { get; } = objectId;
    public IReadOnlyList<ItemEquipEntry> Equipment { get; } = equipment.ToList();

    public byte[] ToBytes()
    {
        if (Equipment.Count > MaximumEquipmentCount)
            throw new InvalidOperationException($"NotifyUpdateRoboEquip cannot contain more than {MaximumEquipmentCount} equipment entries.");

        var writer = new PacketWriter();
        writer.Write(RoboId);
        writer.Write(ObjectId);
        writer.Write((uint)Equipment.Count);
        foreach (var equip in Equipment)
            writer.Write(equip.ToBytes());
        return writer.ToBytes();
    }
}
