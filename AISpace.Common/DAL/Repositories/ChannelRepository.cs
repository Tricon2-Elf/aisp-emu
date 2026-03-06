using AISpace.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public interface IChannelRepository
{
    Task<List<GameChannel>> GetAllAsync(CancellationToken ct = default);
}

public class ChannelRepository(MainContext db) : IChannelRepository
{
    private readonly MainContext _db = db;

    public async Task<List<GameChannel>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Channels.OrderBy(c => c.ChannelNum).ToListAsync(ct);
    }

    public static async Task SeedChannelsIfEmptyAsync(MainContext db, string? ipOverride, ushort areaPort, CancellationToken ct = default)
    {
        if (await db.Channels.AnyAsync(ct))
            return;
        string address = !string.IsNullOrWhiteSpace(ipOverride) ? ipOverride : "localhost";
        db.Channels.Add(
            new GameChannel
            {
                ChannelNum = 1,
                IP = address,
                Port = areaPort,
                CurrentUsers = 0,
                MaxUsers = 1000,
            }
        );
        await db.SaveChangesAsync(ct);
    }
}
