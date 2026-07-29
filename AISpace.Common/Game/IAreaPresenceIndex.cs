namespace AISpace.Common.Game;

public interface IAreaPresenceIndex
{
    IReadOnlyList<IPlayerSession> GetAreaSessions(uint mapId, int channelId);

    IReadOnlyList<IPlayerSession> GetAreaPeers(IPlayerSession session, bool includeSelf = false);

    IPlayerSession? GetAreaSessionByCharacterId(
        uint characterId,
        uint? mapId = null,
        int? channelId = null
    );

    IPlayerSession? GetAreaSessionByUserId(int userId, uint? mapId = null, int? channelId = null);
}
