using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public interface ISessionPresenceRepository
{
    void Upsert(ServerType serverType, IPlayerSession session);
    void Remove(ServerType serverType, Guid connectionId);
    IReadOnlyList<SessionPresence> GetByServerType(ServerType serverType);
    IReadOnlyList<SessionPresence> GetAreaSessions(uint mapId, int channelId);
    SessionPresence? GetAreaSessionByCharacterId(uint characterId, uint? mapId = null, int? channelId = null);
    SessionPresence? GetAreaSessionByUserId(int userId, uint? mapId = null, int? channelId = null);
    int PruneStale(TimeSpan maxAge);
}

public sealed class SessionPresenceRepository(IDbContextFactory<MainContext> factory) : ISessionPresenceRepository
{
    public void Upsert(ServerType serverType, IPlayerSession session)
    {
        using var db = factory.CreateDbContext();
        var now = DateTime.UtcNow;

        if (serverType == ServerType.Area && session.CharacterId != 0)
        {
            var ghosts = db.SessionPresences.Where(row => row.ServerType == ServerType.Area && row.CharacterId == session.CharacterId && row.ConnectionId != session.ConnectionId).ToList();
            if (ghosts.Count > 0)
                db.SessionPresences.RemoveRange(ghosts);
        }

        var existing = db.SessionPresences.SingleOrDefault(row => row.ConnectionId == session.ConnectionId);
        if (existing == null)
        {
            db.SessionPresences.Add(
                new SessionPresence
                {
                    ConnectionId = session.ConnectionId,
                    ServerType = serverType,
                    UserId = session.User?.Id ?? session.UserId,
                    CharacterId = session.CharacterId,
                    MapId = session.MapId,
                    ChannelId = session.ChannelId,
                    X = session.X,
                    Y = session.Y,
                    Z = session.Z,
                    Rotation = session.Rotation,
                    UpdatedAtUtc = now,
                }
            );
        }
        else
        {
            existing.ServerType = serverType;
            existing.UserId = session.User?.Id ?? session.UserId;
            existing.CharacterId = session.CharacterId;
            existing.MapId = session.MapId;
            existing.ChannelId = session.ChannelId;
            existing.X = session.X;
            existing.Y = session.Y;
            existing.Z = session.Z;
            existing.Rotation = session.Rotation;
            existing.UpdatedAtUtc = now;
        }

        db.SaveChanges();
    }

    public void Remove(ServerType serverType, Guid connectionId)
    {
        using var db = factory.CreateDbContext();
        var existing = db.SessionPresences.SingleOrDefault(row => row.ConnectionId == connectionId && row.ServerType == serverType);
        if (existing == null)
            return;

        db.SessionPresences.Remove(existing);
        db.SaveChanges();
    }

    public IReadOnlyList<SessionPresence> GetByServerType(ServerType serverType)
    {
        using var db = factory.CreateDbContext();
        return db.SessionPresences.Where(row => row.ServerType == serverType).ToList();
    }

    public IReadOnlyList<SessionPresence> GetAreaSessions(uint mapId, int channelId)
    {
        using var db = factory.CreateDbContext();
        return db.SessionPresences.Where(row => row.ServerType == ServerType.Area && row.MapId == mapId && (channelId == 0 || row.ChannelId == 0 || row.ChannelId == channelId)).ToList();
    }

    public SessionPresence? GetAreaSessionByCharacterId(uint characterId, uint? mapId = null, int? channelId = null)
    {
        using var db = factory.CreateDbContext();
        var query = db.SessionPresences.Where(row => row.ServerType == ServerType.Area && row.CharacterId == characterId);

        if (mapId.HasValue)
        {
            var requestedChannel = channelId ?? 0;
            query = query.Where(row => row.MapId == mapId.Value && (requestedChannel == 0 || row.ChannelId == 0 || row.ChannelId == requestedChannel));
        }

        return query.FirstOrDefault();
    }

    public SessionPresence? GetAreaSessionByUserId(int userId, uint? mapId = null, int? channelId = null)
    {
        using var db = factory.CreateDbContext();
        var query = db.SessionPresences.Where(row => row.ServerType == ServerType.Area && row.UserId == userId);

        if (mapId.HasValue)
        {
            var requestedChannel = channelId ?? 0;
            query = query.Where(row => row.MapId == mapId.Value && (requestedChannel == 0 || row.ChannelId == 0 || row.ChannelId == requestedChannel));
        }

        return query.FirstOrDefault();
    }

    public int PruneStale(TimeSpan maxAge)
    {
        using var db = factory.CreateDbContext();
        var cutoff = DateTime.UtcNow.Subtract(maxAge);
        var stale = db.SessionPresences.Where(row => row.UpdatedAtUtc < cutoff).ToList();
        if (stale.Count == 0)
            return 0;

        db.SessionPresences.RemoveRange(stale);
        db.SaveChanges();
        return stale.Count;
    }
}
