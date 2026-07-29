using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public interface IPendingMapTransferRepository
{
    void Upsert(SharedState.PendingMapTransfer transfer, TimeSpan ttl);
    bool TryTake(int userId, out SharedState.PendingMapTransfer transfer);
    int CleanupExpired();
}

public sealed class PendingMapTransferRepository(IDbContextFactory<MainContext> factory)
    : IPendingMapTransferRepository
{
    public void Upsert(SharedState.PendingMapTransfer transfer, TimeSpan ttl)
    {
        using var db = factory.CreateDbContext();
        var now = DateTime.UtcNow;
        var existing = db.PendingMapTransfers.SingleOrDefault(row => row.UserId == transfer.UserId);
        if (existing == null)
        {
            db.PendingMapTransfers.Add(
                new PendingMapTransfer
                {
                    UserId = transfer.UserId,
                    MapId = transfer.MapId,
                    MyRoomId = transfer.MyRoomId,
                    ChannelId = transfer.ChannelId,
                    X = transfer.X,
                    Y = transfer.Y,
                    Z = transfer.Z,
                    Rotation = transfer.Rotation,
                    CreatedAtUtc = now,
                    ExpiresAtUtc = now.Add(ttl),
                }
            );
        }
        else
        {
            existing.MapId = transfer.MapId;
            existing.MyRoomId = transfer.MyRoomId;
            existing.ChannelId = transfer.ChannelId;
            existing.X = transfer.X;
            existing.Y = transfer.Y;
            existing.Z = transfer.Z;
            existing.Rotation = transfer.Rotation;
            existing.CreatedAtUtc = now;
            existing.ExpiresAtUtc = now.Add(ttl);
        }

        db.SaveChanges();
    }

    public bool TryTake(int userId, out SharedState.PendingMapTransfer transfer)
    {
        using var db = factory.CreateDbContext();
        using var tx = db.Database.BeginTransaction();
        var row = db.PendingMapTransfers.SingleOrDefault(candidate => candidate.UserId == userId);
        if (row == null || row.ExpiresAtUtc <= DateTime.UtcNow)
        {
            if (row != null)
            {
                db.PendingMapTransfers.Remove(row);
                db.SaveChanges();
            }

            tx.Commit();
            transfer = default;
            return false;
        }

        transfer = new SharedState.PendingMapTransfer(
            row.UserId,
            row.MapId,
            row.ChannelId,
            row.X,
            row.Y,
            row.Z,
            row.Rotation,
            row.MyRoomId
        );
        db.PendingMapTransfers.Remove(row);
        db.SaveChanges();
        tx.Commit();
        return true;
    }

    public int CleanupExpired()
    {
        using var db = factory.CreateDbContext();
        var now = DateTime.UtcNow;
        var expired = db.PendingMapTransfers.Where(row => row.ExpiresAtUtc <= now).ToList();
        if (expired.Count == 0)
            return 0;

        db.PendingMapTransfers.RemoveRange(expired);
        db.SaveChanges();
        return expired.Count;
    }
}
