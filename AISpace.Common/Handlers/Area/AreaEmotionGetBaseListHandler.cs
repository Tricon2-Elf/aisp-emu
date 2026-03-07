using AISpace.Network.Packets.Area;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Common.Game;

namespace AISpace.Common.Handlers.Area;

public class AreaEmotionGetBaseListHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.EmotionGetBaseListRequest;
    public PacketType ResponseType => PacketType.EmotionGetBaseListResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var emotions = new List<EmotionData>();

        // --- ACTION (Category 1) - Human ---
        Add(emotions, 1, "うなずく", 1);
        Add(emotions, 2, "首を横に振る", 1);
        Add(emotions, 3, "転ぶ", 1);
        Add(emotions, 4, "ガッツポーズ", 1);
        Add(emotions, 5, "指をさす", 1);
        Add(emotions, 6, "手を振る", 1);
        Add(emotions, 7, "話す", 1);
        Add(emotions, 8, "恥ずかしがる", 1);
        Add(emotions, 9, "照れる", 1);
        Add(emotions, 10, "がっかりする", 1);
        Add(emotions, 11, "慌てる", 1);
        Add(emotions, 12, "キョロキョロする", 1);
        Add(emotions, 13, "頭を抱える", 1);
        Add(emotions, 17, "ねだる", 1);
        Add(emotions, 18, "驚く", 1);
        Add(emotions, 19, "拍手", 1);
        Add(emotions, 20, "ピース", 1);
        Add(emotions, 21, "迷う", 1);
        Add(emotions, 22, "両手を広げる", 1);
        Add(emotions, 23, "考え事", 1);
        Add(emotions, 24, "威張る", 1);
        Add(emotions, 25, "内緒", 1);
        Add(emotions, 26, "約束", 1);
        for (uint i = 28; i <= 36; i++)
            Add(emotions, i, $"Game {i}", 1);
        Add(emotions, 105, "捕獲", 1);

        // --- PASSION (Category 0) - Heart ---
        Add(emotions, 14, "嬉しい", 0);
        Add(emotions, 15, "哀しい", 0);
        Add(emotions, 16, "泣く", 0);

        // --- ETC (Category 3) - etc ---
        Add(emotions, 27, "座る", 3);
        for (uint i = 100; i <= 104; i++)
            Add(emotions, i, $"Wait {i}", 3);

        // --- VOICE (Category 2) - Note ---
        // Return the base set of voices that worked (Akasaka Hitomi)
        for (uint i = 1; i <= 50; i++)
        {
            uint id = 10101000 + i;
            Add(emotions, id, $"Player Voice {i}", 2);
        }

        var response = new EmotionGetBaseListResponse(0, emotions);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }

    private void Add(List<EmotionData> list, uint id, string name, byte cat)
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
