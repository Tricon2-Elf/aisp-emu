using AISpace.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public interface IChannelRepository
{
    Task<List<GameChannel>> GetAllAsync(CancellationToken ct = default);
    Task<GameChannel?> GetByChannelNumAsync(int channelNum, CancellationToken ct = default);
}

public class ChannelRepository(MainContext db) : IChannelRepository
{
    private readonly MainContext _db = db;

    public async Task<List<GameChannel>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Channels.OrderBy(c => c.ChannelNum).ToListAsync(ct);
    }

    public async Task<GameChannel?> GetByChannelNumAsync(int channelNum, CancellationToken ct = default)
    {
        return await _db.Channels.AsNoTracking().FirstOrDefaultAsync(c => c.ChannelNum == channelNum, ct);
    }

    public static async Task SeedChannelsIfEmptyAsync(MainContext db, string? ipOverride, ushort areaPort, CancellationToken ct = default)
    {
        if (await db.Channels.AnyAsync(ct))
            return;
        string address = !string.IsNullOrWhiteSpace(ipOverride) ? ipOverride : "localhost";
        var num = 1;
        db.Channels.Add(
            new GameChannel
            {
                ChannelNum = num++,
                IP = address,
                Port = areaPort,
                CurrentUsers = 0,
                MaxUsers = 1000,
                MapId = 10990100, //Akihabara
            }
        );
        db.Channels.Add(
            new GameChannel
            {
                ChannelNum = num++,
                IP = address,
                Port = areaPort,
                CurrentUsers = 0,
                MaxUsers = 1000,
                MapId = 10010100, //D.C. II 1
            }
        );
        db.Channels.Add(
            new GameChannel
            {
                ChannelNum = num++,
                IP = address,
                Port = areaPort,
                CurrentUsers = 0,
                MaxUsers = 1000,
                MapId = 10020100, //CLANNAD 1
            }
        );
        db.Channels.Add(
            new GameChannel
            {
                ChannelNum = num++,
                IP = address,
                Port = areaPort,
                CurrentUsers = 0,
                MaxUsers = 1000,
                MapId = 10030100, //SHUFFLE! 1
            }
        );
        await db.SaveChangesAsync(ct);
    }
}
