namespace AISpace.Network;

public interface IOutgoingPacket
{
    byte[] ToBytes();
}

public interface IIncomingPacket<TSelf>
    where TSelf : IIncomingPacket<TSelf>
{
    static abstract TSelf FromBytes(ReadOnlySpan<byte> data);
}

public interface IPacket<TSelf> : IOutgoingPacket, IIncomingPacket<TSelf>
    where TSelf : IPacket<TSelf> { }

public record Packet(ClientConnection Client, PacketType Type, byte[] Data, ushort RawType);
