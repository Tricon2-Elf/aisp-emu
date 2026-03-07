using AISpace.Network.Data;

namespace AISpace.Network.Packets.Msg;

public class AvatarCreateRequest : IPacket<AvatarCreateRequest>
{
    public string AvatarName { get; set; } = string.Empty;
    public uint modelId;
    public CharaVisual visual = new(0, 0, 0, 0, 0, 0, 0);
    public uint slotId;

    public static AvatarCreateRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var createRequest = new AvatarCreateRequest
        {
            AvatarName = reader.ReadString(),
            modelId = reader.ReadUInt(),
            visual = CharaVisual.FromBytes(reader.ReadBytes(19)),
            slotId = reader.ReadUInt(),
        };

        return createRequest;
    }

    public byte[] ToBytes()
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"[AvatarCreateRequest] Name: {AvatarName}, ModelId: {modelId}, SlotId: {slotId}, \r\n\tVisual: {visual}";
    }
}
