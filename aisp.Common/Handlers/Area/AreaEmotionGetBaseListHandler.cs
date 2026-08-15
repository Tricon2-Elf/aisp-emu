using aisp.Common.Game;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaEmotionGetBaseListHandler(ITextLocaliser localiser)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EmotionGetBaseListRequest;
    public PacketType ResponseType => PacketType.EmotionGetBaseListResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var emotions = new List<EmotionData>();

        AddNamed(emotions, session, 1, EmotionCategory.Action);
        AddNamed(emotions, session, 2, EmotionCategory.Action);
        AddNamed(emotions, session, 3, EmotionCategory.Action);
        AddNamed(emotions, session, 4, EmotionCategory.Action);
        AddNamed(emotions, session, 5, EmotionCategory.Action);
        AddNamed(emotions, session, 6, EmotionCategory.Action);
        AddNamed(emotions, session, 7, EmotionCategory.Action);
        AddNamed(emotions, session, 8, EmotionCategory.Action);
        AddNamed(emotions, session, 9, EmotionCategory.Action);
        AddNamed(emotions, session, 10, EmotionCategory.Action);
        AddNamed(emotions, session, 11, EmotionCategory.Action);
        AddNamed(emotions, session, 12, EmotionCategory.Action);
        AddNamed(emotions, session, 13, EmotionCategory.Action);
        AddNamed(emotions, session, 17, EmotionCategory.Action);
        AddNamed(emotions, session, 18, EmotionCategory.Action);
        AddNamed(emotions, session, 19, EmotionCategory.Action);
        AddNamed(emotions, session, 20, EmotionCategory.Action);
        AddNamed(emotions, session, 21, EmotionCategory.Action);
        AddNamed(emotions, session, 22, EmotionCategory.Action);
        AddNamed(emotions, session, 23, EmotionCategory.Action);
        AddNamed(emotions, session, 24, EmotionCategory.Action);
        AddNamed(emotions, session, 25, EmotionCategory.Action);
        AddNamed(emotions, session, 26, EmotionCategory.Action);
        for (uint i = 28; i <= 36; i++)
            Add(
                emotions,
                i,
                localiser.Get(session, L.Emotion.GameFormat, i),
                EmotionCategory.Action
            );
        AddNamed(emotions, session, 105, EmotionCategory.Action);

        AddNamed(emotions, session, 14, EmotionCategory.Passion);
        AddNamed(emotions, session, 15, EmotionCategory.Passion);
        AddNamed(emotions, session, 16, EmotionCategory.Passion);

        AddNamed(emotions, session, 27, EmotionCategory.Etc);
        for (uint i = 100; i <= 104; i++)
            Add(emotions, i, localiser.Get(session, L.Emotion.WaitFormat, i), EmotionCategory.Etc);

        for (uint i = 1; i <= 50; i++)
        {
            uint id = 10101000 + i;
            Add(
                emotions,
                id,
                localiser.Get(session, L.Emotion.VoiceFormat, i),
                EmotionCategory.Voice
            );
        }

        var response = new EmotionGetBaseListResponse(0, emotions);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }

    private void AddNamed(
        List<EmotionData> list,
        IPlayerSession session,
        uint id,
        EmotionCategory cat
    ) => Add(list, id, localiser.Get(session, L.Emotion.Name(id)), cat);

    private static void Add(List<EmotionData> list, uint id, string name, EmotionCategory cat)
    {
        list.Add(
            new EmotionData
            {
                Id = id,
                Name = name,
                Category = cat,
            }
        );
    }
}
