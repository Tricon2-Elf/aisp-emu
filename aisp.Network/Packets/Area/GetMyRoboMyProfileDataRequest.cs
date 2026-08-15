namespace aisp.Network.Packets.Area;

/// <summary>send_get_my_robo_myprofile_data (0x5AA9): roboid.</summary>
public sealed class GetMyRoboMyProfileDataRequest(uint roboId)
    : IIncomingPacket<GetMyRoboMyProfileDataRequest>
{
    public const int WireSize = sizeof(uint);

    public uint RoboId { get; } = roboId;

    public static GetMyRoboMyProfileDataRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(GetMyRoboMyProfileDataRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        var reader = new PacketReader(data);
        return new GetMyRoboMyProfileDataRequest(reader.ReadUInt());
    }
}
