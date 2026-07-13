using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Game.ServerScripts;

public sealed class ShinjuHomeIslandServerScript(ICharacterRepository characterRepository, ICharacterEventRepository characterEventRepository, IMapRepository mapRepository, ServerScriptSession serverScriptSession, Lazy<ServerScriptDispatcher> serverScriptDispatcher, ILogger<ShinjuHomeIslandServerScript> logger) : IServerScript
{
    private static readonly uint[] FranchiseHubMapIds = [10010100, 10020100, 10030100];
    private const uint SelectorFailure = 1;
    private const string IslandSelectStep = "IslandSelect";

    public string EventKey => ServerEvents.Keys.ShinjuHomeIsland;

    public async Task StartAsync(IPlayerSession session, ServerScriptContext context, CancellationToken ct = default)
    {
        var character = await characterRepository.GetByIdAsync((int)session.CharacterId, ct);
        if (character is null)
        {
            logger.LogWarning("Skipping server script {EventKey} for character {CharacterId}: character not found", EventKey, session.CharacterId);
            await serverScriptSession.AbortAsync(session, SelectorFailure, ct);
            return;
        }

        session.Character = character;

        if (await characterEventRepository.HasCompletedAsync((int)session.CharacterId, EventKey, ct))
        {
            var title = await ResolveIslandTitleAsync(character.HomeIslandId, ct);
            await SendDialogueAsync(session, checked((uint)context.Npc.NpcObjectId), context.Npc.Name, $"You're already registered to {title}.", ct);
            await serverScriptSession.CompleteAsync(session, 0, markComplete: false, ct);
            return;
        }

        var npcObjectId = checked((uint)context.Npc.NpcObjectId);
        var state = session.ServerScriptState!;

        await SendDialogueAsync(session, npcObjectId, context.Npc.Name, "Welcome to AI-Space! I'm Shinju. Which island would you like to make your home?", ct);

        var islands = await BuildSelectInitIslandEntriesAsync(ct);
        if (islands.Count == 0)
        {
            logger.LogWarning("Aborting server script {EventKey} for character {CharacterId}: no island entries available", EventKey, session.CharacterId);
            await serverScriptSession.AbortAsync(session, SelectorFailure, ct);
            return;
        }

        state.Step = IslandSelectStep;
        state.Data["npc"] = context.Npc;
        await session.SendAsync(PacketType.SelectInitIslandStart, new SelectInitIslandStartNotify { Islands = islands }.ToBytes(), ct);
    }

    public async Task<bool> TryHandlePacketAsync(PacketType packetType, ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var state = session.ServerScriptState;
        if (state is null || !string.Equals(state.EventKey, EventKey, StringComparison.Ordinal))
            return false;

        if (packetType != PacketType.SelectInitIslandEndRequest || state.Step != IslandSelectStep)
            return false;

        return await OnIslandSelectedAsync(payload, session, state, ct);
    }

    private async Task<bool> OnIslandSelectedAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, ServerScriptState state, CancellationToken ct)
    {
        var request = SelectInitIslandEndRequest.FromBytes(payload.Span);
        if (!IsAllowedIslandId(request.IslandId))
        {
            logger.LogWarning("Rejecting server script {EventKey} for character {CharacterId}: unknown island {IslandId}", EventKey, session.CharacterId, request.IslandId);
            await serverScriptSession.AbortAsync(session, SelectorFailure, ct);
            return true;
        }

        var updated = await characterRepository.UpdateHomeIslandAsync((int)session.CharacterId, request.IslandId, ct);
        if (updated is null)
        {
            logger.LogWarning("Rejecting server script {EventKey} for character {CharacterId}: character not found while saving", EventKey, session.CharacterId);
            await serverScriptSession.AbortAsync(session, SelectorFailure, ct);
            return true;
        }

        session.Character = updated;
        var npc = (Npc)state.Data["npc"];
        logger.LogInformation("Saved home island {IslandId} for character {CharacterId}, starting charadoll event", request.IslandId, session.CharacterId);

        await serverScriptSession.CompleteAsync(session, 0, markComplete: false, ct);
        await serverScriptDispatcher.Value.StartAsync(session, ServerEvents.Keys.ShinjuCharadoll, new ServerScriptContext { Npc = npc, PendingIslandId = request.IslandId }, ct);
        return true;
    }

    private static async Task SendDialogueAsync(IPlayerSession session, uint npcObjectId, string npcName, string text, CancellationToken ct)
    {
        await session.SendAsync(PacketType.EventMessageNotify, new EventMessageNotify(npcObjectId, npcName, text).ToBytes(), ct);
        await session.SendAsync(PacketType.EventMessageCloseNotify, new EventMessageCloseNotify().ToBytes(), ct);
    }

    private async Task<IReadOnlyList<SelectInitIslandEntry>> BuildSelectInitIslandEntriesAsync(CancellationToken ct)
    {
        var islands = new List<SelectInitIslandEntry>(FranchiseHubMapIds.Length);

        foreach (var hubMapId in FranchiseHubMapIds)
        {
            var hubMap = await mapRepository.GetByMapIdAsync(hubMapId, ct);
            if (hubMap is null)
                continue;

            var islandName = string.IsNullOrWhiteSpace(hubMap.Island) ? hubMap.Name : hubMap.Island;
            var relatedMaps = string.IsNullOrWhiteSpace(hubMap.Island) ? [hubMap] : await mapRepository.GetMapsByIslandAsync(hubMap.Island, ct);

            var descriptionLines = relatedMaps.Select(map => map.Name).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList();
            if (descriptionLines.Count == 0)
                descriptionLines.Add(hubMap.Name);

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

    private async Task<string> ResolveIslandTitleAsync(uint islandId, CancellationToken ct)
    {
        var hubMapId = ResolveHubMapId(islandId);
        if (hubMapId is null)
            return $"Island {islandId}";

        var map = await mapRepository.GetByMapIdAsync(hubMapId.Value, ct);
        if (map is null || string.IsNullOrWhiteSpace(map.Island))
            return map?.Name ?? $"Island {islandId}";

        return map.Island;
    }

    private static uint ResolveIslandId(uint hubMapId) => (uint)((hubMapId / 10000) % 100);

    private static bool IsAllowedIslandId(uint islandId) => FranchiseHubMapIds.Any(hubMapId => ResolveIslandId(hubMapId) == islandId);

    private static uint? ResolveHubMapId(uint islandId)
    {
        foreach (var hubMapId in FranchiseHubMapIds)
        {
            if (ResolveIslandId(hubMapId) == islandId)
                return hubMapId;
        }

        return null;
    }
}
