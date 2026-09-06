using System.Text.Json;
using aisp.Common.DAL.Entities;
using aisp.Common.Localisation;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.DAL.Repositories;

public interface IQuestRepository
{
    Task<Quest?> GetByIdAsync(int questId, CancellationToken ct = default);
    Task<IReadOnlyList<Quest>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Quest>> GetAutoStartAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CharacterQuestWork>> GetWorkAsync(
        int characterId,
        CancellationToken ct = default
    );
    Task<IReadOnlyList<CharacterQuestHistory>> GetHistoryAsync(
        int characterId,
        CancellationToken ct = default
    );
    Task<CharacterQuestWork?> GetWorkAsync(
        int characterId,
        int questId,
        CancellationToken ct = default
    );
    Task<bool> HasWorkOrHistoryAsync(int characterId, int questId, CancellationToken ct = default);
    Task<CharacterQuestWork> StartAsync(
        int characterId,
        Quest quest,
        CancellationToken ct = default
    );
    Task<CharacterQuestHistory?> CompleteAsync(
        int characterId,
        int questId,
        byte result = 1,
        CancellationToken ct = default
    );
}

public sealed class QuestRepository(MainContext db) : IQuestRepository
{
    private static readonly JsonSerializerOptions JsonOptions = SeedJson.Options;

    public Task<Quest?> GetByIdAsync(int questId, CancellationToken ct = default) =>
        db.Quests.AsNoTracking().SingleOrDefaultAsync(q => q.Id == questId, ct);

    public async Task<IReadOnlyList<Quest>> GetAllAsync(CancellationToken ct = default) =>
        await db.Quests.AsNoTracking().OrderBy(q => q.Id).ToListAsync(ct);

    public async Task<IReadOnlyList<Quest>> GetAutoStartAsync(CancellationToken ct = default) =>
        await db
            .Quests.AsNoTracking()
            .Where(q => q.AutoStartOnConnect)
            .OrderBy(q => q.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CharacterQuestWork>> GetWorkAsync(
        int characterId,
        CancellationToken ct = default
    ) =>
        await db
            .CharacterQuestWorks.AsNoTracking()
            .Include(x => x.Quest)
            .Where(x => x.CharacterId == characterId)
            .OrderBy(x => x.StartedAtUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CharacterQuestHistory>> GetHistoryAsync(
        int characterId,
        CancellationToken ct = default
    ) =>
        await db
            .CharacterQuestHistories.AsNoTracking()
            .Include(x => x.Quest)
            .Where(x => x.CharacterId == characterId)
            .OrderByDescending(x => x.CompletedAtUtc)
            .ToListAsync(ct);

    public Task<CharacterQuestWork?> GetWorkAsync(
        int characterId,
        int questId,
        CancellationToken ct = default
    ) =>
        db
            .CharacterQuestWorks.Include(x => x.Quest)
            .SingleOrDefaultAsync(x => x.CharacterId == characterId && x.QuestId == questId, ct);

    public async Task<bool> HasWorkOrHistoryAsync(
        int characterId,
        int questId,
        CancellationToken ct = default
    )
    {
        if (
            await db.CharacterQuestWorks.AnyAsync(
                x => x.CharacterId == characterId && x.QuestId == questId,
                ct
            )
        )
            return true;
        return await db.CharacterQuestHistories.AnyAsync(
            x => x.CharacterId == characterId && x.QuestId == questId,
            ct
        );
    }

    public async Task<CharacterQuestWork> StartAsync(
        int characterId,
        Quest quest,
        CancellationToken ct = default
    )
    {
        var existing = await GetWorkAsync(characterId, quest.Id, ct);
        if (existing is not null)
            return existing;

        var work = new CharacterQuestWork
        {
            CharacterId = characterId,
            QuestId = quest.Id,
            Chapter = quest.DefaultChapter,
            RestSec = quest.DefaultRestSec,
            TargetNow = 0,
            TargetRequired = quest.DefaultTargetRequired,
            StartedAtUtc = DateTime.UtcNow,
        };
        db.CharacterQuestWorks.Add(work);
        await db.SaveChangesAsync(ct);
        await db.Entry(work).Reference(x => x.Quest).LoadAsync(ct);
        return work;
    }

    public async Task<CharacterQuestHistory?> CompleteAsync(
        int characterId,
        int questId,
        byte result = 1,
        CancellationToken ct = default
    )
    {
        var work = await db.CharacterQuestWorks.SingleOrDefaultAsync(
            x => x.CharacterId == characterId && x.QuestId == questId,
            ct
        );
        if (work is null)
            return await db
                .CharacterQuestHistories.Include(x => x.Quest)
                .SingleOrDefaultAsync(
                    x => x.CharacterId == characterId && x.QuestId == questId,
                    ct
                );

        var history = await db.CharacterQuestHistories.SingleOrDefaultAsync(
            x => x.CharacterId == characterId && x.QuestId == questId,
            ct
        );
        if (history is null)
        {
            history = new CharacterQuestHistory
            {
                CharacterId = characterId,
                QuestId = questId,
                Chapter = work.Chapter,
                Result = result,
                CompletedAtUtc = DateTime.UtcNow,
            };
            db.CharacterQuestHistories.Add(history);
        }
        else
        {
            history.Chapter = work.Chapter;
            history.Result = result;
            history.CompletedAtUtc = DateTime.UtcNow;
        }

        db.CharacterQuestWorks.Remove(work);
        await db.SaveChangesAsync(ct);
        await db.Entry(history).Reference(x => x.Quest).LoadAsync(ct);
        return history;
    }

    public static async Task SeedQuestsIfEmptyAsync(
        MainContext db,
        string jsonPath,
        CancellationToken ct = default
    )
    {
        if (await db.Quests.AnyAsync(ct))
            return;
        await EnsureSeedQuestsPresentAsync(db, jsonPath, ct);
    }

    public static async Task EnsureSeedQuestsPresentAsync(
        MainContext db,
        string jsonPath,
        CancellationToken ct = default
    )
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException("Quest seed JSON not found.", jsonPath);

        var json = await File.ReadAllTextAsync(jsonPath, ct);
        var rows = JsonSerializer.Deserialize<List<QuestSeedRow>>(json, JsonOptions) ?? [];
        var existing = await db.Quests.ToDictionaryAsync(q => q.Id, ct);
        var locales = new List<(string Key, GameLanguage Language, string Value)>();
        var changed = false;
        foreach (var row in rows.DistinctBy(r => r.Id))
        {
            var chapter = row.Chapter == 0 ? (ushort)1 : row.Chapter;
            var targetRequired = row.TargetRequired == 0 ? (ushort)1 : row.TargetRequired;
            if (existing.TryGetValue(row.Id, out var quest))
            {
                if (
                    quest.Title != row.Title.Canonical
                    || quest.ShortName != row.ShortName.Canonical
                    || quest.Note != row.Note.Canonical
                    || quest.LocationName != row.Location.Canonical
                    || quest.DefaultChapter != chapter
                    || quest.DefaultRestSec != row.RestSec
                    || quest.DefaultTargetName != row.Target.Canonical
                    || quest.DefaultTargetRequired != targetRequired
                    || quest.AutoStartOnConnect != row.AutoStartOnConnect
                )
                {
                    quest.Title = row.Title.Canonical;
                    quest.ShortName = row.ShortName.Canonical;
                    quest.Note = row.Note.Canonical;
                    quest.LocationName = row.Location.Canonical;
                    quest.DefaultChapter = chapter;
                    quest.DefaultRestSec = row.RestSec;
                    quest.DefaultTargetName = row.Target.Canonical;
                    quest.DefaultTargetRequired = targetRequired;
                    quest.AutoStartOnConnect = row.AutoStartOnConnect;
                    changed = true;
                }
            }
            else
            {
                db.Quests.Add(
                    new Quest
                    {
                        Id = row.Id,
                        Title = row.Title.Canonical,
                        ShortName = row.ShortName.Canonical,
                        Note = row.Note.Canonical,
                        LocationName = row.Location.Canonical,
                        DefaultChapter = chapter,
                        DefaultRestSec = row.RestSec,
                        DefaultTargetName = row.Target.Canonical,
                        DefaultTargetRequired = targetRequired,
                        AutoStartOnConnect = row.AutoStartOnConnect,
                    }
                );
                changed = true;
            }

            locales.AddRange(
                LocalisedTextSeeder.FromLocalised(L.Quest.Title(row.Id).Value, row.Title)
            );
            locales.AddRange(
                LocalisedTextSeeder.FromLocalised(L.Quest.ShortName(row.Id).Value, row.ShortName)
            );
            locales.AddRange(
                LocalisedTextSeeder.FromLocalised(L.Quest.Note(row.Id).Value, row.Note)
            );
            locales.AddRange(
                LocalisedTextSeeder.FromLocalised(L.Quest.Location(row.Id).Value, row.Location)
            );
            locales.AddRange(
                LocalisedTextSeeder.FromLocalised(L.Quest.Target(row.Id).Value, row.Target)
            );
        }

        if (changed)
            await db.SaveChangesAsync(ct);
        await LocalisedTextSeeder.UpsertValuesAsync(db, locales, ct);
    }

    private sealed class QuestSeedRow
    {
        public int Id { get; set; }
        public LocalisedString Title { get; set; } = new();
        public LocalisedString ShortName { get; set; } = new();
        public LocalisedString Note { get; set; } = new();
        public LocalisedString Location { get; set; } = new();
        public LocalisedString Target { get; set; } = new();
        public ushort Chapter { get; set; } = 1;
        public uint RestSec { get; set; }
        public ushort TargetRequired { get; set; } = 1;
        public bool AutoStartOnConnect { get; set; }
    }
}
