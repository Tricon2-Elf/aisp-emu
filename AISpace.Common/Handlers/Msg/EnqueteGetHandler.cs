using AISpace.Common.Network.Packets.Msg;
using AISpace.Network;

namespace AISpace.Common.Handlers.Msg;

public class EnqueteGetHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.EnqueteGetRequest;

    public PacketType ResponseType => PacketType.EnqueteGetResponse;

    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        List<EnqueteData> questions = [new EnqueteData(0, "What is the music of life?", ["Um... the lute? No, drums!", "Screaming?", "Silence, my brother", "Some kind of choir. With chanting"])];

        await connection.SendAsync(ResponseType, new EnqueteGetResponse(0, questions), ct);
    }
}
