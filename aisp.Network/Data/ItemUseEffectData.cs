namespace aisp.Network.Data;

/// <summary>
/// The 37-byte <c>notify_use_item_t</c> value read by the client. AvatarData
/// and RoboData each contain eight of these active item-effect slots.
/// </summary>
public sealed class ItemUseEffectData
{
    public const int ParameterCount = 5;
    public const int WireSize = 37;

    public uint ItemSerialId { get; set; }
    public uint Enabled { get; set; }
    public uint ItemDefinitionId { get; set; }
    public uint EffectType { get; set; }
    public uint[] Parameters { get; set; } = new uint[ParameterCount];
    public byte OverwriteExisting { get; set; }

    public byte[] ToBytes()
    {
        if (Parameters.Length != ParameterCount)
            throw new InvalidOperationException(
                $"ItemUseEffectData must contain exactly {ParameterCount} parameters."
            );

        var writer = new PacketWriter();
        writer.Write(ItemSerialId);
        writer.Write(Enabled);
        writer.Write(ItemDefinitionId);
        writer.Write(EffectType);
        foreach (var parameter in Parameters)
            writer.Write(parameter);
        writer.Write(OverwriteExisting);
        return writer.ToBytes();
    }

    public static ItemUseEffectData FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < WireSize)
            throw new ArgumentException(
                $"ItemUseEffectData requires at least {WireSize} bytes.",
                nameof(data)
            );

        var reader = new PacketReader(data);
        var result = new ItemUseEffectData
        {
            ItemSerialId = reader.ReadUInt(),
            Enabled = reader.ReadUInt(),
            ItemDefinitionId = reader.ReadUInt(),
            EffectType = reader.ReadUInt(),
        };
        for (var i = 0; i < result.Parameters.Length; i++)
            result.Parameters[i] = reader.ReadUInt();
        result.OverwriteExisting = reader.ReadByte();
        return result;
    }
}
