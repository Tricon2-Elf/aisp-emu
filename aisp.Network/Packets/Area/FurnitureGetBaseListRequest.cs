using aisp.Network;

namespace aisp.Network.Packets.Area;

public class FurnitureGetBaseListRequest : IIncomingPacket<FurnitureGetBaseListRequest>
{
    public static FurnitureGetBaseListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (!data.IsEmpty)
            throw new InvalidDataException(
                $"{nameof(FurnitureGetBaseListRequest)} requires an empty payload, received {data.Length} bytes."
            );

        return new FurnitureGetBaseListRequest();
    }
}
