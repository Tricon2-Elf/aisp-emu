using System.Text.Json;
using aisp.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.DAL.Repositories;

public interface IChannelRepository
{
    Task<List<GameChannel>> GetAllAsync(CancellationToken ct = default);
    Task<GameChannel?> GetByChannelNumAsync(int channelNum, CancellationToken ct = default);
}

public class ChannelRepository(MainContext db) : IChannelRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly MainContext _db = db;

    public async Task<List<GameChannel>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Channels.OrderBy(c => c.ChannelNum).ToListAsync(ct);
    }

    public async Task<GameChannel?> GetByChannelNumAsync(
        int channelNum,
        CancellationToken ct = default
    )
    {
        return await _db
            .Channels.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ChannelNum == channelNum, ct);
    }

    public static async Task SeedChannelsIfEmptyAsync(
        MainContext db,
        string jsonPath,
        string? ipOverride,
        ushort areaPort,
        CancellationToken ct = default
    )
    {
        if (await db.Channels.AnyAsync(ct))
            return;

        if (!File.Exists(jsonPath))
            throw new FileNotFoundException(
                "Channel seed JSON not found (required for empty Channels table).",
                jsonPath
            );

        var json = await File.ReadAllTextAsync(jsonPath, ct);
        var rows = JsonSerializer.Deserialize<List<ChannelSeedRow>>(json, JsonOptions) ?? [];
        string address = !string.IsNullOrWhiteSpace(ipOverride) ? ipOverride : "localhost";

        foreach (var row in rows)
        {
            db.Channels.Add(
                new GameChannel
                {
                    ChannelNum = row.ChannelNum,
                    IP = address,
                    Port = areaPort,
                    CurrentUsers = row.CurrentUsers,
                    MaxUsers = row.MaxUsers,
                    MapId = row.MapId,
                }
            );
        }

        await db.SaveChangesAsync(ct);
    }

    private sealed class ChannelSeedRow
    {
        public int ChannelNum { get; set; }
        public uint MapId { get; set; }
        public uint CurrentUsers { get; set; }
        public uint MaxUsers { get; set; } = 1000;
    }
}
