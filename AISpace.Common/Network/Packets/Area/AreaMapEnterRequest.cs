namespace AISpace.Common.Network.Packets.Area;

public class AreaMapEnterRequest : IPacket<AreaMapEnterRequest>
{
    public static AreaMapEnterRequest FromBytes(ReadOnlySpan<byte> data) => new AreaMapEnterRequest();
    public byte[] ToBytes() => throw new NotImplementedException();
}