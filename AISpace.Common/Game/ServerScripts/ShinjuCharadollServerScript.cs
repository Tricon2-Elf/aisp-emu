using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Game.ServerScripts;

public sealed class ShinjuCharadollServerScript(ICharacterRepository characterRepository, ServerScriptSession serverScriptSession, ILogger<ShinjuCharadollServerScript> logger) : IServerScript
{
    private static readonly (CharadollPersonality Personality, string Label)[] Options =
    [
        (CharadollPersonality.Active, "活発そうなタイプ (Active/Energetic)"),
        (CharadollPersonality.Quiet, "おとなし目なタイプ (Quiet/Reserved)"),
        (CharadollPersonality.None, "特に好みはない (No preference)"),
    ];

    private const uint SelectorFailure = 1;
    private const string SelectStep = "Select";

    public string EventKey => ServerEvents.Keys.ShinjuCharadoll;

    public async Task StartAsync(IPlayerSession session, ServerScriptContext context, CancellationToken ct = default)
    {
        if (context.PendingIslandId is not uint islandId || islandId == 0)
        {
            logger.LogWarning("Aborting server script {EventKey} for character {CharacterId}: missing pending island id", EventKey, session.CharacterId);
            await serverScriptSession.AbortAsync(session, SelectorFailure, ct);
            return;
        }

        var character = await characterRepository.GetByIdAsync((int)session.CharacterId, ct);
        if (character is null)
        {
            logger.LogWarning("Skipping server script {EventKey} for character {CharacterId}: character not found", EventKey, session.CharacterId);
            await serverScriptSession.AbortAsync(session, SelectorFailure, ct);
            return;
        }

        session.Character = character;
        var state = session.ServerScriptState!;
        state.Step = SelectStep;
        state.Data["islandId"] = islandId;

        var npcObjectId = checked((uint)context.Npc.NpcObjectId);
        await SendDialogueAsync(session, npcObjectId, context.Npc.Name, "どのキャラドールがお好みですか？", ct);
        await session.SendAsync(PacketType.EventSelectInitNotify, new EventSelectInitNotify { SelectType = EventSelectType.Dialogue }.ToBytes(), ct);
        foreach (var (_, label) in Options)
            await session.SendAsync(PacketType.EventSelectPushNotify, new EventSelectPushNotify { SelectName = label }.ToBytes(), ct);
        await session.SendAsync(PacketType.EventSelectExecNotify, new EventSelectExecNotify { Text = "キャラドールのタイプを選んでください。" }.ToBytes(), ct);
    }

    public async Task<bool> TryHandlePacketAsync(PacketType packetType, ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var state = session.ServerScriptState;
        if (state is null || !string.Equals(state.EventKey, EventKey, StringComparison.Ordinal))
            return false;

        if (packetType != PacketType.EventSelectExecRRequest || state.Step != SelectStep)
            return false;

        return await OnCharadollSelectedAsync(payload, session, state, ct);
    }

    private async Task<bool> OnCharadollSelectedAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, ServerScriptState state, CancellationToken ct)
    {
        var request = EventSelectExecRRequest.FromBytes(payload.Span);
        if (request.Result != 0)
        {
            logger.LogInformation("Server script {EventKey} cancelled for character {CharacterId}: client result {Result}", EventKey, session.CharacterId, request.Result);
            await serverScriptSession.AbortAsync(session, request.Result, ct);
            return true;
        }

        if (request.SelectNo >= Options.Length)
        {
            logger.LogWarning("Rejecting server script {EventKey} for character {CharacterId}: invalid charadoll selection {SelectNo}", EventKey, session.CharacterId, request.SelectNo);
            await serverScriptSession.AbortAsync(session, SelectorFailure, ct);
            return true;
        }

        var islandId = (uint)state.Data["islandId"];
        var personality = Options[request.SelectNo].Personality;
        var updated = await characterRepository.CompleteHomeRegistrationAsync((int)session.CharacterId, islandId, personality, ct);
        if (updated is null)
        {
            logger.LogWarning("Rejecting server script {EventKey} for character {CharacterId}: character not found while saving", EventKey, session.CharacterId);
            await serverScriptSession.AbortAsync(session, SelectorFailure, ct);
            return true;
        }

        session.Character = updated;
        logger.LogInformation("Registered charadoll personality {Personality} for character {CharacterId} on island {IslandId}", personality, session.CharacterId, islandId);
        await serverScriptSession.CompleteAsync(session, 0, markComplete: true, completionEventKey: ServerEvents.Keys.ShinjuHomeIsland, ct);
        return true;
    }

    private static async Task SendDialogueAsync(IPlayerSession session, uint npcObjectId, string npcName, string text, CancellationToken ct)
    {
        await session.SendAsync(PacketType.EventMessageNotify, new EventMessageNotify(npcObjectId, npcName, text).ToBytes(), ct);
        await session.SendAsync(PacketType.EventMessageCloseNotify, new EventMessageCloseNotify().ToBytes(), ct);
    }
}
