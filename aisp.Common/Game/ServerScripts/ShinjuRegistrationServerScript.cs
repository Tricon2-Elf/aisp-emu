using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game.ServerScripts;

public sealed class ShinjuRegistrationServerScript(
    ICharacterRepository characterRepository,
    ICharacterEventRepository characterEventRepository,
    IMapRepository mapRepository,
    ServerScriptSession serverScriptSession,
    ITextLocaliser localiser,
    ILogger<ShinjuRegistrationServerScript> logger
) : IServerScript
{
    private static readonly uint[] FranchiseHubMapIds = [10010100, 10020100, 10030100];
    private static readonly (CharadollPersonality Personality, LocKey Label)[] CharadollOptions =
    [
        (CharadollPersonality.Active, L.Script.Shinju.CharadollActive),
        (CharadollPersonality.Quiet, L.Script.Shinju.CharadollQuiet),
        (CharadollPersonality.None, L.Script.Shinju.CharadollNone),
    ];

    private const uint SelectorFailure = 1;
    private const string HelpPromptStep = "HelpPrompt";
    private const string IslandSelectStep = "IslandSelect";
    private const string CharadollSelectStep = "CharadollSelect";

    public string EventKey => ServerEvents.Keys.ShinjuRegistration;

    public async Task StartAsync(
        IPlayerSession session,
        ServerScriptContext context,
        CancellationToken ct = default
    )
    {
        var character = await characterRepository.GetByIdAsync((int)session.CharacterId, ct);
        if (character is null)
        {
            logger.LogWarning(
                "Skipping server script {EventKey} for character {CharacterId}: character not found",
                EventKey,
                session.CharacterId
            );
            await serverScriptSession.AbortAsync(session, SelectorFailure, ct);
            return;
        }

        session.Character = character;

        if (
            await characterEventRepository.HasCompletedAsync((int)session.CharacterId, EventKey, ct)
        )
        {
            session.ServerScriptState!.Step = HelpPromptStep;
            await SendDialogueAsync(
                session,
                checked((uint)context.Npc.NpcObjectId),
                NpcName(session, context.Npc),
                localiser.Get(session, L.Script.Shinju.Help),
                ct
            );
            await session.SendAsync(
                PacketType.EventSyncNotify,
                new EventSyncNotify().ToBytes(),
                ct
            );
            return;
        }

        var state = session.ServerScriptState!;
        state.Data["npc"] = context.Npc;

        if (character.HomeIslandId > 0)
        {
            logger.LogInformation(
                "Resuming charadoll selection for character {CharacterId} on island {IslandId}",
                session.CharacterId,
                character.HomeIslandId
            );
            await StartCharadollSelectAsync(session, context.Npc, character.HomeIslandId, ct);
            return;
        }

        await StartIslandSelectAsync(session, context.Npc, ct);
    }

    public async Task<bool> TryHandlePacketAsync(
        PacketType packetType,
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var state = session.ServerScriptState;
        if (state is null || !string.Equals(state.EventKey, EventKey, StringComparison.Ordinal))
            return false;

        return packetType switch
        {
            PacketType.EventSyncRRequest when state.Step == HelpPromptStep =>
                await OnHelpPromptClosedAsync(payload, session, ct),
            PacketType.SelectInitIslandEndRequest when state.Step == IslandSelectStep =>
                await OnIslandSelectedAsync(payload, session, state, ct),
            PacketType.EventSelectExecRRequest when state.Step == CharadollSelectStep =>
                await OnCharadollSelectedAsync(payload, session, state, ct),
            _ => false,
        };
    }

    private async Task<bool> OnHelpPromptClosedAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct
    )
    {
        var request = EventSyncRRequest.FromBytes(payload.Span);
        await serverScriptSession.CompleteAsync(session, request.Result, markComplete: false, ct);
        return true;
    }

    private async Task StartIslandSelectAsync(IPlayerSession session, Npc npc, CancellationToken ct)
    {
        var npcObjectId = checked((uint)npc.NpcObjectId);
        await SendDialogueAsync(
            session,
            npcObjectId,
            NpcName(session, npc),
            localiser.Get(session, L.Script.Shinju.Welcome),
            ct
        );

        var islands = await BuildSelectInitIslandEntriesAsync(session, ct);
        if (islands.Count == 0)
        {
            logger.LogWarning(
                "Aborting server script {EventKey} for character {CharacterId}: no island entries available",
                EventKey,
                session.CharacterId
            );
            await serverScriptSession.AbortAsync(session, SelectorFailure, ct);
            return;
        }

        session.ServerScriptState!.Step = IslandSelectStep;
        await session.SendAsync(
            PacketType.SelectInitIslandStart,
            new SelectInitIslandStartNotify { Islands = islands }.ToBytes(),
            ct
        );
    }

    private async Task StartCharadollSelectAsync(
        IPlayerSession session,
        Npc npc,
        uint islandId,
        CancellationToken ct
    )
    {
        var state = session.ServerScriptState!;
        state.Step = CharadollSelectStep;
        state.Data["islandId"] = islandId;

        var npcObjectId = checked((uint)npc.NpcObjectId);
        await SendDialogueAsync(
            session,
            npcObjectId,
            NpcName(session, npc),
            localiser.Get(session, L.Script.Shinju.CharadollQuestion),
            ct
        );
        await session.SendAsync(
            PacketType.EventSelectInitNotify,
            new EventSelectInitNotify { SelectType = EventSelectType.Dialogue }.ToBytes(),
            ct
        );
        foreach (var (_, label) in CharadollOptions)
            await session.SendAsync(
                PacketType.EventSelectPushNotify,
                new EventSelectPushNotify { SelectName = localiser.Get(session, label) }.ToBytes(),
                ct
            );
        await session.SendAsync(
            PacketType.EventSelectExecNotify,
            new EventSelectExecNotify
            {
                Text = localiser.Get(session, L.Script.Shinju.CharadollPrompt),
            }.ToBytes(),
            ct
        );
    }

    private async Task<bool> OnIslandSelectedAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        ServerScriptState state,
        CancellationToken ct
    )
    {
        var request = SelectInitIslandEndRequest.FromBytes(payload.Span);
        if (!IsAllowedIslandId(request.IslandId))
        {
            logger.LogWarning(
                "Rejecting server script {EventKey} for character {CharacterId}: unknown island {IslandId}",
                EventKey,
                session.CharacterId,
                request.IslandId
            );
            await serverScriptSession.AbortAsync(session, SelectorFailure, ct);
            return true;
        }

        var updated = await characterRepository.UpdateHomeIslandAsync(
            (int)session.CharacterId,
            request.IslandId,
            ct
        );
        if (updated is null)
        {
            logger.LogWarning(
                "Rejecting server script {EventKey} for character {CharacterId}: character not found while saving",
                EventKey,
                session.CharacterId
            );
            await serverScriptSession.AbortAsync(session, SelectorFailure, ct);
            return true;
        }

        session.Character = updated;
        var npc = (Npc)state.Data["npc"];
        logger.LogInformation(
            "Saved home island {IslandId} for character {CharacterId}, continuing to charadoll selection",
            request.IslandId,
            session.CharacterId
        );
        await StartCharadollSelectAsync(session, npc, request.IslandId, ct);
        return true;
    }

    private async Task<bool> OnCharadollSelectedAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        ServerScriptState state,
        CancellationToken ct
    )
    {
        var request = EventSelectExecRRequest.FromBytes(payload.Span);
        if (request.Result != 0)
        {
            logger.LogInformation(
                "Server script {EventKey} cancelled for character {CharacterId}: client result {Result}",
                EventKey,
                session.CharacterId,
                request.Result
            );
            await serverScriptSession.AbortAsync(session, request.Result, ct);
            return true;
        }

        if (request.SelectNo >= CharadollOptions.Length)
        {
            logger.LogWarning(
                "Rejecting server script {EventKey} for character {CharacterId}: invalid charadoll selection {SelectNo}",
                EventKey,
                session.CharacterId,
                request.SelectNo
            );
            await serverScriptSession.AbortAsync(session, SelectorFailure, ct);
            return true;
        }

        var islandId = (uint)state.Data["islandId"];
        var personality = CharadollOptions[request.SelectNo].Personality;
        var updated = await characterRepository.CompleteHomeRegistrationAsync(
            (int)session.CharacterId,
            islandId,
            personality,
            ct
        );
        if (updated is null)
        {
            logger.LogWarning(
                "Rejecting server script {EventKey} for character {CharacterId}: character not found while saving",
                EventKey,
                session.CharacterId
            );
            await serverScriptSession.AbortAsync(session, SelectorFailure, ct);
            return true;
        }

        session.Character = updated;
        logger.LogInformation(
            "Registered charadoll personality {Personality} for character {CharacterId} on island {IslandId}",
            personality,
            session.CharacterId,
            islandId
        );
        await serverScriptSession.CompleteAsync(session, 0, markComplete: true, ct);
        return true;
    }

    private static async Task SendDialogueAsync(
        IPlayerSession session,
        uint npcObjectId,
        string npcName,
        string text,
        CancellationToken ct
    )
    {
        await SendMessageAsync(session, npcObjectId, npcName, text, ct);
        await session.SendAsync(
            PacketType.EventMessageCloseNotify,
            new EventMessageCloseNotify().ToBytes(),
            ct
        );
    }

    private static Task SendMessageAsync(
        IPlayerSession session,
        uint npcObjectId,
        string npcName,
        string text,
        CancellationToken ct
    ) =>
        session.SendAsync(
            PacketType.EventMessageNotify,
            new EventMessageNotify(npcObjectId, npcName, text).ToBytes(),
            ct
        );

    private string NpcName(IPlayerSession session, Npc npc) =>
        localiser.Get(session, L.Npc.Name(npc.NpcObjectId));

    private async Task<IReadOnlyList<SelectInitIslandEntry>> BuildSelectInitIslandEntriesAsync(
        IPlayerSession session,
        CancellationToken ct
    )
    {
        var islands = new List<SelectInitIslandEntry>(FranchiseHubMapIds.Length);

        foreach (var hubMapId in FranchiseHubMapIds)
        {
            var hubMap = await mapRepository.GetByMapIdAsync(hubMapId, ct);
            if (hubMap is null)
                continue;

            var islandName = localiser.Get(session, L.Map.Island(hubMap.MapId));
            var relatedMaps = string.IsNullOrWhiteSpace(hubMap.Island)
                ? [hubMap]
                : await mapRepository.GetMapsByIslandAsync(hubMap.Island, ct);

            var descriptionLines = relatedMaps
                .Select(map => localiser.Get(session, L.Map.Name(map.MapId)))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList();
            if (descriptionLines.Count == 0)
                descriptionLines.Add(localiser.Get(session, L.Map.Name(hubMap.MapId)));

            islands.Add(
                new SelectInitIslandEntry
                {
                    IslandId = ResolveIslandId(hubMapId),
                    Title = islandName,
                    Description = string.Join("\n", descriptionLines),
                }
            );
        }

        return islands.OrderBy(island => island.IslandId).ToList();
    }

    private static uint ResolveIslandId(uint hubMapId) => (uint)((hubMapId / 10000) % 100);

    private static bool IsAllowedIslandId(uint islandId) =>
        FranchiseHubMapIds.Any(hubMapId => ResolveIslandId(hubMapId) == islandId);
}
