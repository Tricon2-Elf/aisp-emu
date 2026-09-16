using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public sealed class InventoryCapacityTests
{
    [Fact]
    public async Task AddInventoryAsync_AllowsExistingStackForLegacyOverCapacityInventory()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        var ct = TestContext.Current.CancellationToken;
        await TestDb.SeedCharacterAsync(options, 1, ct);

        await using var db = new MainContext(options);
        for (var index = 0; index <= CharacterRepository.MaximumInventoryStacks; index++)
        {
            var itemId = 34_000_000 + index;
            db.Items.Add(new Item { Id = itemId, Name = $"Legacy Item {index}" });
        }
        await db.SaveChangesAsync(ct);

        for (var index = 0; index <= CharacterRepository.MaximumInventoryStacks; index++)
        {
            var itemId = 34_000_000 + index;
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO CharacterInventory (CharacterId, ItemId, Quantity) VALUES ({1}, {itemId}, {1})",
                ct
            );
        }

        var repository = new CharacterRepository(db, NullLogger<CharacterRepository>.Instance);
        Assert.True(await repository.AddInventoryAsync(1, 34_000_000, 1, ct));
        Assert.False(await repository.AddInventoryAsync(1, 35_000_000, 1, ct));

        await using var verify = new MainContext(options);
        Assert.Equal(
            2,
            await verify
                .CharacterInventories.Where(x => x.CharacterId == 1 && x.ItemId == 34_000_000)
                .Select(x => x.Quantity)
                .SingleAsync(ct)
        );
        Assert.False(
            await verify.CharacterInventories.AnyAsync(
                x => x.CharacterId == 1 && x.ItemId == 35_000_000,
                ct
            )
        );
    }

    [Fact]
    public async Task AddInventoryAsync_RejectsInventoryAboveMaximumStacks()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        var ct = TestContext.Current.CancellationToken;
        await TestDb.SeedCharacterAsync(options, 1, ct);

        await using (var db = new MainContext(options))
        {
            for (var index = 0; index <= CharacterRepository.MaximumInventoryStacks; index++)
            {
                var itemId = 30_000_000 + index;
                db.Items.Add(new Item { Id = itemId, Name = $"Capacity Item {index}" });
            }
            await db.SaveChangesAsync(ct);

            var repository = new CharacterRepository(db, NullLogger<CharacterRepository>.Instance);
            var items = Enumerable
                .Range(0, CharacterRepository.MaximumInventoryStacks + 1)
                .ToDictionary(index => 30_000_000 + index, _ => 1);
            Assert.False(await repository.AddInventoryAsync(1, items, ct));
        }

        await using var verify = new MainContext(options);
        Assert.Empty(await verify.CharacterInventories.ToListAsync(ct));
    }

    [Fact]
    public async Task SaveChangesAsync_AllowsQuantityChangeAtMaximumStacks()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var _ = connection;
        var ct = TestContext.Current.CancellationToken;
        await TestDb.SeedCharacterAsync(options, 1, ct);

        await using var db = new MainContext(options);
        for (var index = 0; index < CharacterRepository.MaximumInventoryStacks; index++)
        {
            var itemId = 31_000_000 + index;
            db.Items.Add(new Item { Id = itemId, Name = $"Full Item {index}" });
            db.CharacterInventories.Add(
                new CharacterInventory
                {
                    CharacterId = 1,
                    ItemId = itemId,
                    Quantity = 1,
                }
            );
        }
        await db.SaveChangesAsync(ct);

        var existing = await db.CharacterInventories.FirstAsync(ct);
        existing.Quantity++;
        await db.SaveChangesAsync(ct);

        Assert.Equal(
            CharacterRepository.MaximumInventoryStacks,
            await db.CharacterInventories.CountAsync(ct)
        );
        Assert.Equal(2, existing.Quantity);
    }
}
