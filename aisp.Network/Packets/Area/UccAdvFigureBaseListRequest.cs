namespace aisp.Network.Packets.Area;

public class UccAdvFigureBaseListRequest : IIncomingPacket<UccAdvFigureBaseListRequest>
{
    public static UccAdvFigureBaseListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (!data.IsEmpty)
            throw new InvalidDataException(
                $"{nameof(UccAdvFigureBaseListRequest)} requires an empty payload, received {data.Length} bytes."
            );

        return new UccAdvFigureBaseListRequest();
    }
}
