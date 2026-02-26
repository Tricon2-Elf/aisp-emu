using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Msg;

public class CircleGetDataRequest : IPacket<CircleGetDataRequest>
{
    // У этого запроса обычно нет параметров, он просто запрашивает список
    public static CircleGetDataRequest FromBytes(ReadOnlySpan<byte> data)
    {
        return new CircleGetDataRequest();
    }

    public byte[] ToBytes() => Array.Empty<byte>();
}