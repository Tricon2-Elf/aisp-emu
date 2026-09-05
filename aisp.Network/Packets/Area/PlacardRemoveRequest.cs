namespace aisp.Network.Packets.Area;

/// <summary>Removes the current player's Friend Link placard.</summary>
public sealed class PlacardRemoveRequest : IIncomingPacket<PlacardRemoveRequest>
{
    public static PlacardRemoveRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (!data.IsEmpty)
            throw new InvalidDataException("Placard remove requests must not contain a payload.");

        return new PlacardRemoveRequest();
    }
}
