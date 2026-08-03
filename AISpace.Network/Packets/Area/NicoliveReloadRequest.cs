namespace AISpace.Network.Packets.Area;

/// <summary>send_nicolive_reload (0x5D63): requests the current live-program ID.</summary>
public sealed class NicoliveReloadRequest : IIncomingPacket<NicoliveReloadRequest>
{
    public static NicoliveReloadRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (!data.IsEmpty)
            throw new InvalidDataException(
                $"{nameof(NicoliveReloadRequest)} requires an empty payload, received {data.Length} bytes."
            );

        return new NicoliveReloadRequest();
    }
}
