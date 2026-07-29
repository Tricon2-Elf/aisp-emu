namespace AISpace.Network.Data;

public sealed class AvatarData(uint avatarId, CharaData character)
{
    public const int WireSize = 928;
    public const int ItemUseEffectCount = 8;

    public uint AvatarId { get; set; } = avatarId;
    public CharaData Character { get; set; } = character;
    public ItemUseEffectData[] ItemUseEffects { get; set; } =
        Enumerable.Range(0, ItemUseEffectCount).Select(_ => new ItemUseEffectData()).ToArray();

    /// <summary>
    /// A protocol-reserved UInt32. ReadAvatarData consumes it, but this client
    /// build never consults the stored value.
    /// </summary>
    public uint ClientReserved { get; set; }

    public uint EmotionId { get; set; }
    public byte RoboVoiceType { get; set; }
    public UserStatusData UserStatus { get; set; } = new();

    public byte[] ToBytes()
    {
        if (ItemUseEffects.Length != ItemUseEffectCount)
            throw new InvalidOperationException(
                $"AvatarData must contain exactly {ItemUseEffectCount} item-use effects."
            );

        PacketWriter writer = new();
        writer.Write(AvatarId);
        writer.Write(Character.ToBytes());
        foreach (var effect in ItemUseEffects)
            writer.Write(effect.ToBytes());
        writer.Write(ClientReserved);
        writer.Write(EmotionId);
        writer.Write(RoboVoiceType);
        writer.Write(UserStatus.ToBytes());
        return writer.ToBytes();
    }

    public static AvatarData FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < WireSize)
            throw new ArgumentException(
                $"AvatarData requires at least {WireSize} bytes.",
                nameof(data)
            );

        var reader = new PacketReader(data);
        var avatarId = reader.ReadUInt();
        var character = CharaData.FromBytes(reader.ReadBytes(CharaData.WireSize));
        var effects = new ItemUseEffectData[ItemUseEffectCount];
        for (var i = 0; i < effects.Length; i++)
            effects[i] = ItemUseEffectData.FromBytes(reader.ReadBytes(ItemUseEffectData.WireSize));

        return new AvatarData(avatarId, character)
        {
            ItemUseEffects = effects,
            ClientReserved = reader.ReadUInt(),
            EmotionId = reader.ReadUInt(),
            RoboVoiceType = reader.ReadByte(),
            UserStatus = UserStatusData.FromBytes(reader.ReadBytes(UserStatusData.WireSize)),
        };
    }
}
