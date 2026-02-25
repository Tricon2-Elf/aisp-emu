namespace AISpace.Common.Network.Packets.Area;

public class AreaMapEnterRequest : IPacket<AreaMapEnterRequest>
{
    public uint MapID { get; set; }

    public static AreaMapEnterRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new AreaMapEnterRequest 
        { 
            MapID = reader.ReadUInt() 
        };
    }

    public byte[] ToBytes() => throw new NotImplementedException();
}
