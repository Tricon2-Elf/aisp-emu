using aisp.Common.Game;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class EnqueteGetHandler(ITextLocaliser localiser)
    : IPacketHandler,
        IRequiresAuthenticatedSession
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
                localiser.Get(session, L.Enquete.MusicQuestion),
                [
                    localiser.Get(session, L.Enquete.MusicAnswer0),
                    localiser.Get(session, L.Enquete.MusicAnswer1),
                    localiser.Get(session, L.Enquete.MusicAnswer2),
                    localiser.Get(session, L.Enquete.MusicAnswer3),
                ]
            ),
        ];

        await session.SendAsync(ResponseType, new EnqueteGetResponse(0, questions).ToBytes(), ct);
    }
}
