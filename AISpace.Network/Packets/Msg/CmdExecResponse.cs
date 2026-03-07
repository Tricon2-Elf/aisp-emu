using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CmdExecResponse(uint messageId, uint result) : IPacket<CmdExecResponse>
{
    public uint MessageId = messageId;
    public uint Result = result;

    public static CmdExecResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(MessageId);
        writer.Write(Result);
        return writer.ToBytes();
    }
}
