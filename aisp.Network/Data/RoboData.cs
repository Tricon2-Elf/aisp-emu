namespace aisp.Network.Data;

public sealed class RoboData(uint roboId, CharaData character, uint state = 0)
{
    public const int WireSize = 961;
    public const int ItemUseEffectCount = 8;
    public const int DistributedStatusPointCount = 5;

    public uint RoboId { get; set; } = roboId;

    /// <summary>
    /// Avatar object ID that owns this Robo. The client uses this value to
    /// distinguish the current player's Robo from other players' Robos.
    /// </summary>
    public uint OwnerAvatarId { get; set; }

    public uint State { get; set; } = state;

    /// <summary>
    /// A protocol-reserved UInt32. ReadRoboData consumes it, but this client
    /// build never consults the stored value.
    /// </summary>
    public uint ClientReserved { get; set; }

    public ushort AiScriptId { get; set; }
    public CharaData Character { get; set; } = character;
    public ItemUseEffectData[] ItemUseEffects { get; set; } =
        Enumerable.Range(0, ItemUseEffectCount).Select(_ => new ItemUseEffectData()).ToArray();
    public uint EmotionId { get; set; }
    public uint AvailableStatusPoints { get; set; }
    public uint[] DistributedStatusPoints { get; set; } = new uint[DistributedStatusPointCount];
    public UserStatusData UserStatus { get; set; } = new();

    public CharaData Chara
    {
        get => Character;
        set => Character = value;
    }

    public byte[] ToBytes()
    {
        if (ItemUseEffects.Length != ItemUseEffectCount)
            throw new InvalidOperationException(
                $"RoboData must contain exactly {ItemUseEffectCount} item-use effects."
            );
        if (DistributedStatusPoints.Length != DistributedStatusPointCount)
            throw new InvalidOperationException(
                $"RoboData must contain exactly {DistributedStatusPointCount} distributed status-point values."
            );

        PacketWriter writer = new();
        writer.Write(RoboId);
        writer.Write(OwnerAvatarId);
        writer.Write(State);
        writer.Write(ClientReserved);
        writer.Write(AiScriptId);
        writer.Write(Character.ToBytes());
        foreach (var effect in ItemUseEffects)
            writer.Write(effect.ToBytes());
        writer.Write(EmotionId);
        writer.Write(AvailableStatusPoints);
        foreach (var points in DistributedStatusPoints)
            writer.Write(points);
        writer.Write(UserStatus.ToBytes());
        return writer.ToBytes();
    }

    public static RoboData FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < WireSize)
            throw new ArgumentException(
                $"RoboData requires at least {WireSize} bytes.",
                nameof(data)
            );

        var reader = new PacketReader(data);
        var roboId = reader.ReadUInt();
        var ownerAvatarId = reader.ReadUInt();
        var state = reader.ReadUInt();
        var clientReserved = reader.ReadUInt();
        var aiScriptId = reader.ReadUShort();
        var character = CharaData.FromBytes(reader.ReadBytes(CharaData.WireSize));
        var effects = new ItemUseEffectData[ItemUseEffectCount];
        for (var i = 0; i < effects.Length; i++)
            effects[i] = ItemUseEffectData.FromBytes(reader.ReadBytes(ItemUseEffectData.WireSize));

        var result = new RoboData(roboId, character, state)
        {
            OwnerAvatarId = ownerAvatarId,
            ClientReserved = clientReserved,
            AiScriptId = aiScriptId,
            ItemUseEffects = effects,
            EmotionId = reader.ReadUInt(),
            AvailableStatusPoints = reader.ReadUInt(),
        };
        for (var i = 0; i < result.DistributedStatusPoints.Length; i++)
            result.DistributedStatusPoints[i] = reader.ReadUInt();
        result.UserStatus = UserStatusData.FromBytes(reader.ReadBytes(UserStatusData.WireSize));
        return result;
    }
}
