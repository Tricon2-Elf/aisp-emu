namespace AISpace.Common.Network;

public interface IPacket<TSelf> where TSelf : IPacket<TSelf>
{

    byte[] ToBytes();
    static abstract TSelf FromBytes(ReadOnlySpan<byte> data);
}


public record Packet(ClientConnection Client, PacketType Type, byte[] Data, ushort RawType);
