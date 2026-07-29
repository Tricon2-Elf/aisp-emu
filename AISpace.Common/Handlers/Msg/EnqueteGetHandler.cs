using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public class EnqueteGetHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EnqueteGetRequest;

    public PacketType ResponseType => PacketType.EnqueteGetResponse;

    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        List<EnqueteData> questions =
        [
            new EnqueteData(
                0,
                "What is the music of life?",
                [
                    "Um... the lute? No, drums!",
                    "Screaming?",
                    "Silence, my brother",
                    "Some kind of choir. With chanting",
                ]
            ),
        ];

        await session.SendAsync(ResponseType, new EnqueteGetResponse(0, questions).ToBytes(), ct);
    }
}
