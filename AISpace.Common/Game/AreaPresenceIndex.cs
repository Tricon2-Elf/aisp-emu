namespace AISpace.Common.Game;

public sealed class AreaPresenceIndex(ISessionClientRegistry sessionClientRegistry) : IAreaPresenceIndex
{
    public IReadOnlyList<IPlayerSession> GetAreaSessions(uint mapId, int channelId)
    {
        return sessionClientRegistry.GetClients(ServerType.Area).Where(session => IsInArea(session, mapId, channelId)).ToList();
    }

    public IReadOnlyList<IPlayerSession> GetAreaPeers(IPlayerSession session, bool includeSelf = false)
    {
        var peers = GetAreaSessions(session.MapId, session.ChannelId);
        return includeSelf ? peers : peers.Where(other => other.ConnectionId != session.ConnectionId).ToList();
    }

    public IPlayerSession? GetAreaSessionByCharacterId(uint characterId, uint? mapId = null, int? channelId = null)
    {
        IEnumerable<IPlayerSession> candidates = sessionClientRegistry.GetClients(ServerType.Area).Where(session => session.CharacterId == characterId);

        if (mapId.HasValue)
            candidates = candidates.Where(session => IsInArea(session, mapId.Value, channelId ?? 0));

        return candidates.FirstOrDefault();
    }

    public IPlayerSession? GetAreaSessionByUserId(int userId, uint? mapId = null, int? channelId = null)
    {
        IEnumerable<IPlayerSession> candidates = sessionClientRegistry.GetClients(ServerType.Area).Where(session => (session.User?.Id ?? session.UserId) == userId);

        if (mapId.HasValue)
            candidates = candidates.Where(session => IsInArea(session, mapId.Value, channelId ?? 0));

        return candidates.FirstOrDefault();
    }

    private static bool IsInArea(IPlayerSession session, uint mapId, int channelId)
    {
        if (session.MapId != mapId)
            return false;

        if (channelId == 0 || session.ChannelId == 0)
            return true;

        return session.ChannelId == channelId;
    }
}
