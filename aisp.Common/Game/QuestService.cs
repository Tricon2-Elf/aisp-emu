using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Game;

public interface IQuestService
{
    Task OnPlayerConnectedAsync(
        IPlayerSession session,
        CancellationToken ct = default,
        bool pushTracker = true
    );
    Task<QuestGetWorkResponse> GetWorkResponseAsync(
        IPlayerSession session,
        CancellationToken ct = default
    );
    Task<QuestGetHistoryResponse> GetHistoryResponseAsync(
        IPlayerSession session,
        CancellationToken ct = default
    );
    Task CompleteAsync(
        IPlayerSession session,
        int questId,
        byte result = 1,
        CancellationToken ct = default
    );
}

public sealed class QuestService(IQuestRepository quests, ITextLocaliser localiser) : IQuestService
{
    public async Task OnPlayerConnectedAsync(
        IPlayerSession session,
        CancellationToken ct = default,
        bool pushTracker = true
    )
    {
        var characterId = ResolveCharacterId(session);
        if (characterId == 0)
            return;

        await EnsureAutoStartAsync(characterId, ct);
        if (!pushTracker)
            return;

        foreach (var work in await quests.GetWorkAsync(characterId, ct))
            await SendStartedAsync(session, work, ct);
    }

    public async Task<QuestGetWorkResponse> GetWorkResponseAsync(
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var characterId = ResolveCharacterId(session);
        if (characterId == 0)
            return new QuestGetWorkResponse(0, []);

        await EnsureAutoStartAsync(characterId, ct);
        var work = await quests.GetWorkAsync(characterId, ct);
        return new QuestGetWorkResponse(0, work.Select(row => ToWorkData(session, row)).ToList());
    }

    public async Task<QuestGetHistoryResponse> GetHistoryResponseAsync(
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var characterId = ResolveCharacterId(session);
        if (characterId == 0)
            return new QuestGetHistoryResponse(0, []);

        var history = await quests.GetHistoryAsync(characterId, ct);
        return new QuestGetHistoryResponse(
            0,
            history.Select(row => ToHistoryData(session, row)).ToList()
        );
    }

    public async Task CompleteAsync(
        IPlayerSession session,
        int questId,
        byte result = 1,
        CancellationToken ct = default
    )
    {
        var characterId = ResolveCharacterId(session);
        if (characterId == 0)
            return;
        if (await quests.GetWorkAsync(characterId, questId, ct) is null)
            return;

        var history = await quests.CompleteAsync(characterId, questId, result, ct);
        if (history is null)
            return;

        await session.SendAsync(
            PacketType.QuestAddHistoryNotify,
            new QuestAddHistoryNotify(ToHistoryData(session, history)).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.QuestEndedNotify,
            new QuestEndedNotify((uint)questId).ToBytes(),
            ct
        );
    }

    private async Task EnsureAutoStartAsync(int characterId, CancellationToken ct)
    {
        foreach (var definition in await quests.GetAutoStartAsync(ct))
        {
            if (await quests.HasWorkOrHistoryAsync(characterId, definition.Id, ct))
                continue;
            await quests.StartAsync(characterId, definition, ct);
        }
    }

    private static int ResolveCharacterId(IPlayerSession session)
    {
        if (session.CharacterId != 0)
            return checked((int)session.CharacterId);
        return session.Character?.Id ?? 0;
    }

    private async Task SendStartedAsync(
        IPlayerSession session,
        CharacterQuestWork work,
        CancellationToken ct
    )
    {
        var data = ToWorkData(session, work);
        await session.SendAsync(
            PacketType.QuestStartedNotify,
            new QuestStartedNotify(data).ToBytes(),
            ct
        );
        if (work.TargetRequired == 0)
            return;

        await session.SendAsync(
            PacketType.QuestSetTargetNotify,
            new QuestSetTargetNotify(
                data.Base.QuestId,
                work.TargetRequired,
                data.TargetName
            ).ToBytes(),
            ct
        );
        if (work.TargetNow == 0)
            return;

        await session.SendAsync(
            PacketType.QuestUpdateTargetNotify,
            new QuestUpdateTargetNotify(data.Base.QuestId, work.TargetNow).ToBytes(),
            ct
        );
    }

    private QuestWorkData ToWorkData(IPlayerSession session, CharacterQuestWork work)
    {
        var quest = work.Quest;
        return new QuestWorkData
        {
            Base = ToBase(session, quest, work.Chapter),
            RestSec = work.RestSec,
            LocationName = Resolve(session, L.Quest.Location(quest.Id), quest.LocationName),
            TargetName = Resolve(session, L.Quest.Target(quest.Id), quest.DefaultTargetName),
            Now = work.TargetNow,
            Required = work.TargetRequired,
        };
    }

    private QuestHistoryData ToHistoryData(IPlayerSession session, CharacterQuestHistory history) =>
        new()
        {
            Base = ToBase(session, history.Quest, history.Chapter),
            RelatedQuestId = 0,
            Result = history.Result == 0 ? (byte)1 : history.Result,
        };

    private QuestBaseData ToBase(IPlayerSession session, Quest quest, ushort chapter) =>
        new()
        {
            QuestId = (uint)quest.Id,
            Title = Resolve(session, L.Quest.Title(quest.Id), quest.Title),
            ShortName = Resolve(session, L.Quest.ShortName(quest.Id), quest.ShortName),
            Chapter = chapter,
            Note = Resolve(session, L.Quest.Note(quest.Id), quest.Note),
        };

    private string Resolve(IPlayerSession session, LocKey key, string fallback)
    {
        if (
            localiser.TryGet(session.Language, key, out var value)
            && !string.IsNullOrWhiteSpace(value)
            && value != key.Value
        )
            return value;
        if (
            session.Language != GameLanguage.Japanese
            && localiser.TryGet(GameLanguage.Japanese, key, out var japanese)
            && !string.IsNullOrWhiteSpace(japanese)
            && japanese != key.Value
        )
            return japanese;
        return fallback;
    }
}
