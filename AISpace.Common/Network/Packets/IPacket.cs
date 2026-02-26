namespace AISpace.Common.Network.Packets;

public interface IPacket<TSelf>
    where TSelf : IPacket<TSelf>
{
    //void Write(ref PacketWriter writer);

    //static abstract TSelf Read(ref PacketReader reader);

    byte[] ToBytes();
    static abstract TSelf FromBytes(ReadOnlySpan<byte> data);
}
