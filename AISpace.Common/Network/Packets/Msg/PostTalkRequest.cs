using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets;

public class PostTalkRequest(uint messageID, uint distID, string message, uint balloonID) : IPacket<PostTalkRequest>
{
    public uint MessageID = messageID;
    public uint DistID = distID;
    public string Message = message;
    public uint BalloonID = balloonID;

    public static PostTalkRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        uint msgId = reader.ReadUInt();
        uint distId = reader.ReadUInt();
        string msg = reader.ReadString("Shift_JIS"); 
        uint balloonId = reader.ReadUInt();
        return new PostTalkRequest(msgId, distId, msg, balloonId);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(MessageID);
        writer.Write(DistID);
        writer.Write(Message, "Shift_JIS"); 
        writer.Write(BalloonID);
        return writer.ToBytes();
    }
}