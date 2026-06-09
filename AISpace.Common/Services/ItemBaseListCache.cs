using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Services;

public interface IItemBaseListCache
{
    ReadOnlyMemory<byte> ResponsePayload { get; }

    Task WarmAsync(CancellationToken ct = default);
}

public sealed class ItemBaseListCache(IItemRepository itemRepo) : IItemBaseListCache
{
    private byte[]? _payload;

    public ReadOnlyMemory<byte> ResponsePayload => _payload ?? ReadOnlyMemory<byte>.Empty;

    public async Task WarmAsync(CancellationToken ct = default)
    {
        var rows = await itemRepo.GetAllAsync(ct);
        var items = rows.Select(ItemEntityMapper.ToItemBaseListData).ToList();
        _payload = new ItemGetBaseListResponse(0, items).ToBytes();
    }
}
