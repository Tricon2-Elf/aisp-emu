using aisp.Common.DAL.Entities;
using aisp.Network;
using aisp.Network.Data;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.DAL.Repositories;

public interface INicotvRepository
{
    Task<Nicotv?> GetOrCreateForFurnitureAsync(
        int roomId,
        uint furnitureId,
        CancellationToken ct = default
    );
    Task<Nicotv?> UpdateForFurnitureAsync(
        int roomId,
        uint furnitureId,
        NicotvData data,
        CancellationToken ct = default
    );
    Task<Nicotv?> GetByIdInRoomAsync(int roomId, uint nicotvId, CancellationToken ct = default);
    Task<Nicotv?> GetByIdAsync(uint nicotvId, CancellationToken ct = default);
    Task<IReadOnlyList<Nicotv>> GetByChannelAsync(uint channelId, CancellationToken ct = default);
    Task<Nicotv?> SetChannelAsync(
        int roomId,
        uint nicotvId,
        uint channelId,
        CancellationToken ct = default
    );
    Task<Nicotv?> SetPlaybackStateAsync(
        int roomId,
        uint nicotvId,
        NicotvPlaybackState playbackState,
        CancellationToken ct = default
    );
    Task<Nicotv?> SetMovieAsync(
        int roomId,
        uint nicotvId,
        string movieId,
        CancellationToken ct = default
    );
    Task<Nicotv?> CloseAsync(int roomId, uint nicotvId, CancellationToken ct = default);
}

public sealed class NicotvRepository(MainContext db) : INicotvRepository
{
    public async Task<Nicotv?> GetOrCreateForFurnitureAsync(
        int roomId,
        uint furnitureId,
        CancellationToken ct = default
    )
    {
        var existing = await db.Nicotvs.SingleOrDefaultAsync(
            x => x.RoomId == roomId && x.FurnitureId == furnitureId,
            ct
        );
        if (existing is not null)
            return existing;

        if (
            !await db.MyRoomFurniture.AnyAsync(
                x => x.RoomId == roomId && x.FurnitureId == furnitureId,
                ct
            )
        )
            return null;

        var nicotv = new Nicotv { RoomId = roomId, FurnitureId = furnitureId };
        db.Nicotvs.Add(nicotv);
        await db.SaveChangesAsync(ct);
        return nicotv;
    }

    public async Task<Nicotv?> UpdateForFurnitureAsync(
        int roomId,
        uint furnitureId,
        NicotvData data,
        CancellationToken ct = default
    )
    {
        if (
            !Enum.IsDefined(data.PlaybackState)
            || !Enum.IsDefined(data.CommentVisibility)
            || data.MovieId.Length > NicotvData.MovieIdLength - 1
        )
            return null;

        var nicotv = await GetOrCreateForFurnitureAsync(roomId, furnitureId, ct);
        if (nicotv is null)
            return null;

        nicotv.ChannelId = data.ChannelId;
        nicotv.MovieId = data.MovieId;
        nicotv.PlaybackState = data.PlaybackState;
        nicotv.CommentVisibility = data.CommentVisibility;
        nicotv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return nicotv;
    }

    public async Task<Nicotv?> GetByIdInRoomAsync(
        int roomId,
        uint nicotvId,
        CancellationToken ct = default
    )
    {
        if (nicotvId == 0 || nicotvId > int.MaxValue)
            return null;

        return await db.Nicotvs.SingleOrDefaultAsync(
            x => x.RoomId == roomId && x.Id == checked((int)nicotvId),
            ct
        );
    }

    public async Task<Nicotv?> GetByIdAsync(uint nicotvId, CancellationToken ct = default)
    {
        if (nicotvId == 0 || nicotvId > int.MaxValue)
            return null;

        return await db.Nicotvs.SingleOrDefaultAsync(x => x.Id == checked((int)nicotvId), ct);
    }

    public async Task<IReadOnlyList<Nicotv>> GetByChannelAsync(
        uint channelId,
        CancellationToken ct = default
    ) =>
        channelId == 0 ? [] : await db.Nicotvs.Where(x => x.ChannelId == channelId).ToListAsync(ct);

    public async Task<Nicotv?> SetChannelAsync(
        int roomId,
        uint nicotvId,
        uint channelId,
        CancellationToken ct = default
    )
    {
        var nicotv = await GetByIdInRoomAsync(roomId, nicotvId, ct);
        if (nicotv is null)
            return null;

        // A TV shows a channel or a typed movie, never both. The movie id takes precedence when
        // resolving what a TV shows (a typed video is the more specific pick), so tuning a
        // channel clears it, and re-entering the room shows the tuned channel.
        nicotv.ChannelId = channelId;
        nicotv.MovieId = "";
        nicotv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return nicotv;
    }

    public async Task<Nicotv?> SetPlaybackStateAsync(
        int roomId,
        uint nicotvId,
        NicotvPlaybackState playbackState,
        CancellationToken ct = default
    )
    {
        if (!Enum.IsDefined(playbackState))
            return null;

        var nicotv = await GetByIdInRoomAsync(roomId, nicotvId, ct);
        if (nicotv is null)
            return null;

        nicotv.PlaybackState = playbackState;
        nicotv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return nicotv;
    }

    public async Task<Nicotv?> SetMovieAsync(
        int roomId,
        uint nicotvId,
        string movieId,
        CancellationToken ct = default
    )
    {
        if (movieId.Length > NicotvData.MovieIdLength - 1)
            return null;

        var nicotv = await GetByIdInRoomAsync(roomId, nicotvId, ct);
        if (nicotv is null)
            return null;

        // Exclusive with a channel selection; see the matching note in SetChannelAsync.
        nicotv.MovieId = movieId;
        nicotv.ChannelId = 0;
        nicotv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return nicotv;
    }

    public async Task<Nicotv?> CloseAsync(int roomId, uint nicotvId, CancellationToken ct = default)
    {
        var nicotv = await GetByIdInRoomAsync(roomId, nicotvId, ct);
        if (nicotv is null)
            return null;

        nicotv.PlaybackState = NicotvPlaybackState.Closed;
        nicotv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return nicotv;
    }
}
