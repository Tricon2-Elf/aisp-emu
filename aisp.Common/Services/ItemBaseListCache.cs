using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Localisation;
using aisp.Network.Packets.Msg;
using Microsoft.Extensions.DependencyInjection;

namespace aisp.Common.Services;

public interface IItemBaseListCache
{
    ReadOnlyMemory<byte> GetResponsePayload(GameLanguage language);

    Task WarmAsync(CancellationToken ct = default);
    Task<bool> ContainsItemAsync(int itemId, CancellationToken ct = default);
}

public sealed class ItemBaseListCache(IServiceScopeFactory scopeFactory) : IItemBaseListCache
{
    private readonly Dictionary<GameLanguage, byte[]> _payloads = [];
    private HashSet<int> _itemIds = [];

    public ReadOnlyMemory<byte> GetResponsePayload(GameLanguage language)
    {
        if (_payloads.TryGetValue(language, out var payload))
            return payload;
        if (_payloads.TryGetValue(GameLanguage.Japanese, out var japanese))
            return japanese;
        return ReadOnlyMemory<byte>.Empty;
    }

    public async Task WarmAsync(CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var itemRepo = scope.ServiceProvider.GetRequiredService<IItemRepository>();
        var localiser = scope.ServiceProvider.GetRequiredService<ITextLocaliser>();
        var rows = await itemRepo.GetAllAsync(ct);
        _itemIds = rows.Select(r => r.Id).ToHashSet();

        foreach (var language in GameLanguages.All)
        {
            var items = rows.Select(row =>
                {
                    var name = localiser.Get(language, L.Item.Name(row.Id));
                    var description = localiser.Get(language, L.Item.Description(row.Id));
                    var limitDescription = localiser.Get(language, L.Item.LimitDescription(row.Id));
                    return ItemEntityMapper.ToItemBaseListData(
                        row,
                        name,
                        description,
                        limitDescription
                    );
                })
                .ToList();
            _payloads[language] = new ItemGetBaseListResponse(0, items).ToBytes();
        }
    }

    public async Task<bool> ContainsItemAsync(int itemId, CancellationToken ct = default)
    {
        if (itemId <= 0)
            return false;

        if (_payloads.Count == 0)
            await WarmAsync(ct);

        return _itemIds.Contains(itemId);
    }
}
