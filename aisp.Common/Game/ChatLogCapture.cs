using aisp.Common.DAL.Entities;

namespace aisp.Common.Game;

public static class ChatLogCapture
{
    public static ChatMessage FromSession(
        IPlayerSession session,
        SharedState state,
        ChatLogKind kind,
        string message,
        uint distId = 0,
        uint balloonId = 0,
        int? circleId = null,
        bool rejected = false
    )
    {
        var area = state.GetAreaSessionByUserId(session.UserId);
        var characterId = session.CharacterId != 0 ? session.CharacterId : area?.CharacterId ?? 0;
        var name = session.Character?.Name ?? area?.Character?.Name ?? string.Empty;
        return new ChatMessage
        {
            Kind = kind,
            UserId = session.UserId,
            CharacterId = checked((int)characterId),
            CharacterName = name,
            Message = message,
            DistId = distId,
            BalloonId = balloonId,
            CircleId = circleId,
            MapId = area?.MapId ?? (session.MapId == 0 ? null : session.MapId),
            ChannelId = area?.ChannelId ?? (session.ChannelId == 0 ? null : session.ChannelId),
            Rejected = rejected,
        };
    }
}
