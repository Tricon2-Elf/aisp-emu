using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaEmotionGetBaseListHandler : IPacketHandler, IRequiresAuthenticatedSession
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

        // --- ACTION ---
        Add(emotions, 1, "うなずく", EmotionCategory.Action);
        Add(emotions, 2, "首を横に振る", EmotionCategory.Action);
        Add(emotions, 3, "転ぶ", EmotionCategory.Action);
        Add(emotions, 4, "ガッツポーズ", EmotionCategory.Action);
        Add(emotions, 5, "指をさす", EmotionCategory.Action);
        Add(emotions, 6, "手を振る", EmotionCategory.Action);
        Add(emotions, 7, "話す", EmotionCategory.Action);
        Add(emotions, 8, "恥ずかしがる", EmotionCategory.Action);
        Add(emotions, 9, "照れる", EmotionCategory.Action);
        Add(emotions, 10, "がっかりする", EmotionCategory.Action);
        Add(emotions, 11, "慌てる", EmotionCategory.Action);
        Add(emotions, 12, "キョロキョロする", EmotionCategory.Action);
        Add(emotions, 13, "頭を抱える", EmotionCategory.Action);
        Add(emotions, 17, "ねだる", EmotionCategory.Action);
        Add(emotions, 18, "驚く", EmotionCategory.Action);
        Add(emotions, 19, "拍手", EmotionCategory.Action);
        Add(emotions, 20, "ピース", EmotionCategory.Action);
        Add(emotions, 21, "迷う", EmotionCategory.Action);
        Add(emotions, 22, "両手を広げる", EmotionCategory.Action);
        Add(emotions, 23, "考え事", EmotionCategory.Action);
        Add(emotions, 24, "威張る", EmotionCategory.Action);
        Add(emotions, 25, "内緒", EmotionCategory.Action);
        Add(emotions, 26, "約束", EmotionCategory.Action);
        for (uint i = 28; i <= 36; i++)
            Add(emotions, i, $"Game {i}", EmotionCategory.Action);
        Add(emotions, 105, "捕獲", EmotionCategory.Action);

        // --- PASSION ---
        Add(emotions, 14, "嬉しい", EmotionCategory.Passion);
        Add(emotions, 15, "哀しい", EmotionCategory.Passion);
        Add(emotions, 16, "泣く", EmotionCategory.Passion);

        // --- ETC ---
        Add(emotions, 27, "座る", EmotionCategory.Etc);
        for (uint i = 100; i <= 104; i++)
            Add(emotions, i, $"Wait {i}", EmotionCategory.Etc);

        // --- VOICE ---
        for (uint i = 1; i <= 50; i++)
        {
            uint id = 10101000 + i;
            Add(emotions, id, $"Player Voice {i}", EmotionCategory.Voice);
        }

        var response = new EmotionGetBaseListResponse(0, emotions);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }

    private void Add(List<EmotionData> list, uint id, string name, EmotionCategory cat)
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
