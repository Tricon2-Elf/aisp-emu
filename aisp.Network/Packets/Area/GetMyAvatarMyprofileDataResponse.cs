using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public class GetMyAvatarMyprofileDataResponse(
    ProfileData pData,
    AvatarProfileMetadata metadata = default
) : IOutgoingPacket
{
    public AvatarProfileMetadata Metadata { get; } = metadata;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0);
        AvatarProfile.Write(writer, pData, Metadata);
        return writer.ToBytes();
    }
}
