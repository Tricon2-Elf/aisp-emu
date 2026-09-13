using aisp.Network;

namespace aisp.Network.Packets.Area;

public class NiconiCommonsBaseListRequest : IIncomingPacket<NiconiCommonsBaseListRequest>
{
    public static NiconiCommonsBaseListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (!data.IsEmpty)
            throw new InvalidDataException(
                $"{nameof(NiconiCommonsBaseListRequest)} requires an empty payload, received {data.Length} bytes."
            );

        return new NiconiCommonsBaseListRequest();
    }
}
