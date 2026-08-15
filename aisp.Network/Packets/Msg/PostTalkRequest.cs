using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class PostTalkRequest(uint messageID, uint distID, string message, uint balloonID)
    : IIncomingPacket<PostTalkRequest>
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
        string msg = reader.ReadString();
        uint balloonId = reader.ReadUInt();
        return new PostTalkRequest(msgId, distId, msg, balloonId);
    }
}
