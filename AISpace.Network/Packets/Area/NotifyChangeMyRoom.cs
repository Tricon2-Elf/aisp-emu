using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class NotifyChangeMyRoom(string ip, ushort port, uint mapId, uint mapSerialId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();

        writer.Write(0f);
        writer.Write(0.1f);
        writer.Write(0f);
        writer.Write(0f);
        writer.Write(new byte[16]);
        writer.Write((byte)0);
        writer.Write(port);
        writer.WriteFixedAsciiString(ip, 65);
        writer.Write((byte)0);
        writer.Write(mapId);
        writer.Write(mapSerialId);
        writer.Write((uint)9);
        writer.Write(new byte[64]);
        writer.Write((byte)1);
        return writer.ToBytes();
    }
}
