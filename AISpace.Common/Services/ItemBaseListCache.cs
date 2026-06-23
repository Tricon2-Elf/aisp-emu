using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network.Packets.Msg;
using Microsoft.Extensions.DependencyInjection;

namespace AISpace.Common.Services;

public interface IItemBaseListCache
{
    ReadOnlyMemory<byte> ResponsePayload { get; }

    Task WarmAsync(CancellationToken ct = default);
    Task<bool> ContainsItemAsync(int itemId, CancellationToken ct = default);
}

public sealed class ItemBaseListCache(IServiceScopeFactory scopeFactory) : IItemBaseListCache
{
    private byte[]? _payload;
    private HashSet<int> _itemIds = [];

    public ReadOnlyMemory<byte> ResponsePayload => _payload ?? ReadOnlyMemory<byte>.Empty;

    public async Task WarmAsync(CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var itemRepo = scope.ServiceProvider.GetRequiredService<IItemRepository>();
        var rows = await itemRepo.GetAllAsync(ct);
        var items = rows.Select(ItemEntityMapper.ToItemBaseListData).ToList();
        _itemIds = rows.Select(r => r.Id).ToHashSet();
        _payload = new ItemGetBaseListResponse(0, items).ToBytes();
    }

    public async Task<bool> ContainsItemAsync(int itemId, CancellationToken ct = default)
    {
        if (itemId <= 0)
            return false;

        if (_payload is null)
            await WarmAsync(ct);

        return _itemIds.Contains(itemId);
    }
}
