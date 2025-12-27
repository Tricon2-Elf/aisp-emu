
using AISpace.Common.Game;
using AISpace.Common.Network.Packets.Msg;

namespace AISpace.Common.Network.Handlers.Msg;

public class EnqueteGetHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.Msg_EnqueteGetRequest;

    public PacketType ResponseType => PacketType.Msg_EnqueteGetResponse;

    public MessageDomain Domains => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        List<EnqueteData> questions = [new EnqueteData(0, "What is the music of life?", ["Um... the lute? No, drums!", "Screaming?", "Silence, my brother", "Some kind of choir. With chanting"])];

        await connection.SendAsync(ResponseType, new EnqueteGetResponse(0, questions), ct);
    }
}
