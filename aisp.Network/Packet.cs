namespace aisp.Network;

public interface IOutgoingPacket
{
    byte[] ToBytes();
}

public interface IIncomingPacket<TSelf>
    where TSelf : IIncomingPacket<TSelf>
{
    static abstract TSelf FromBytes(ReadOnlySpan<byte> data);
}

public record Packet(ClientConnection Client, PacketType Type, byte[] Data, ushort RawType);
