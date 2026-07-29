using AISpace.Common.DAL.Repositories;
using AISpace.Common.Handlers.Area;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Game.ServerScripts;

public sealed class MyRoomDoorServerScript(ClientScriptSegmentRunner clientScriptSegmentRunner, ServerScriptSession serverScriptSession, DirectMapLinkTransitionService directMapLinkTransitionService, ICharacterEventRepository characterEventRepository, IMyRoomRepository myRoomRepository, ILogger<MyRoomDoorServerScript> logger) : IServerScript
{
    public const uint AkihabaraUdxMapId = 40_990_200;

    private static readonly IReadOnlyDictionary<uint, uint> ShoppingAreaMapIds = new Dictionary<uint, uint>
    {
        [1] = 10_010_200,
        [2] = 10_020_200,
        [3] = 10_030_200,
    };

    private const uint EventFailure = 1;
    private const string SegmentPhaseDataKey = "myroomDoor.segmentPhase";
    private const string ReturnMyRoomMapDataKey = "myroomDoor.returnMyRoomMapId";
    private const string ReturnMyRoomIdDataKey = "myroomDoor.returnMyRoomId";
    private const string UdxMapDataReadyDataKey = "myroomDoor.udxMapDataReady";
    private const string UdxMapEnterAcknowledgedDataKey = "myroomDoor.udxMapEnterAcknowledged";
    private const string CompletedDoorSelectionStep = "completedDoorSelection";
    private const string PhaseCharadoll = "charadoll";
    private const string PhaseTpsBat0101011 = "tpsBat0101011";
    private const string PhaseTpsBat0101012 = "tpsBat0101012";
    private const string PhaseAwaitingUdxMapReady = "awaitingUdxMapReady";
    private const string PhaseTpsBat0101021 = "tpsBat0101021";

    public string EventKey => ServerEvents.Keys.MyRoomDoor;

    public Task<bool> CanStartAsync(IPlayerSession session, ServerScriptContext context, CancellationToken ct = default) => Task.FromResult(session.CharacterId != 0);

    public async Task StartAsync(IPlayerSession session, ServerScriptContext context, CancellationToken ct = default)
    {
        var state = session.ServerScriptState!;
        if (await characterEventRepository.HasCompletedAsync(checked((int)session.CharacterId), EventKey, ct))
        {
            state.Step = CompletedDoorSelectionStep;
            await session.SendAsync(PacketType.EventSelectInitNotify, new EventSelectInitNotify { SelectType = EventSelectType.Popup }.ToBytes(), ct);
            await session.SendAsync(PacketType.EventSelectPushNotify, new EventSelectPushNotify { SelectName = "Return to Shopping Area" }.ToBytes(), ct);
            await session.SendAsync(PacketType.EventSelectPushNotify, new EventSelectPushNotify { SelectName = "Close" }.ToBytes(), ct);
            await session.SendAsync(PacketType.EventSelectExecNotify, new EventSelectExecNotify { Text = "Leave MyRoom?" }.ToBytes(), ct);
            return;
        }

        state.Data[ReturnMyRoomMapDataKey] = MyRoomInfo.IsMyRoomMap(session.MapId) ? session.MapId : MyRoomInfo.BaseMapId;
        state.Data[ReturnMyRoomIdDataKey] = session.MyRoomId;
        state.Data[SegmentPhaseDataKey] = PhaseCharadoll;
        logger.LogInformation("Starting client script segment {ClientScriptKey} for character {CharacterId} on My Room door", ScriptedEvents.Keys.SysEvent002, session.CharacterId);
        await clientScriptSegmentRunner.BeginAsync(session, ScriptedEvents.Keys.SysEvent002, ct);
    }

    public async Task<bool> TryHandlePacketAsync(PacketType packetType, ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var state = session.ServerScriptState;
        if (state is null || !string.Equals(state.EventKey, EventKey, StringComparison.Ordinal))
            return false;

        if (state.Step == CompletedDoorSelectionStep && packetType == PacketType.EventSelectExecRRequest)
            return await HandleCompletedDoorSelectionAsync(payload, session, ct);

        var phase = state.Data.TryGetValue(SegmentPhaseDataKey, out var rawPhase) ? rawPhase as string : null;
        if (string.Equals(phase, PhaseAwaitingUdxMapReady, StringComparison.Ordinal) && packetType is PacketType.MapDataEnterEndRequest or PacketType.MapEnterRequest)
            return await TryResumeAfterUdxMapReadyAsync(packetType, session, state, ct);

        var segmentResult = await clientScriptSegmentRunner.TryHandleAsync(packetType, payload, session, ct);
        switch (segmentResult.Status)
        {
            case ClientScriptSegmentStatus.NotHandled:
                return false;
            case ClientScriptSegmentStatus.InProgress:
                return true;
            case ClientScriptSegmentStatus.Failed:
                logger.LogWarning("Aborting server script {EventKey} for character {CharacterId}: client script result {Result}", EventKey, session.CharacterId, segmentResult.Result);
                await serverScriptSession.AbortAsync(session, segmentResult.Result == 0 ? EventFailure : segmentResult.Result, ct);
                return true;
            case ClientScriptSegmentStatus.Completed:
                return await AdvanceAfterSegmentAsync(session, state, phase, ct);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task<bool> AdvanceAfterSegmentAsync(IPlayerSession session, ServerScriptState state, string? phase, CancellationToken ct)
    {
        if (string.Equals(phase, PhaseCharadoll, StringComparison.Ordinal))
        {
            state.Data[SegmentPhaseDataKey] = PhaseTpsBat0101011;
            logger.LogInformation("Starting client script segment {ClientScriptKey} for character {CharacterId} after sys_event_002", ScriptedEvents.Keys.TpsEventBat0101011, session.CharacterId);
            await clientScriptSegmentRunner.BeginAsync(session, ScriptedEvents.Keys.TpsEventBat0101011, ct);
            return true;
        }

        if (string.Equals(phase, PhaseTpsBat0101011, StringComparison.Ordinal))
        {
            state.Data[SegmentPhaseDataKey] = PhaseTpsBat0101012;
            logger.LogInformation("Starting client script segment {ClientScriptKey} for character {CharacterId} after {PreviousKey}", ScriptedEvents.Keys.Bat0101012, session.CharacterId, ScriptedEvents.Keys.TpsEventBat0101011);
            await clientScriptSegmentRunner.BeginAsync(session, ScriptedEvents.Keys.Bat0101012, ct);
            return true;
        }

        if (string.Equals(phase, PhaseTpsBat0101012, StringComparison.Ordinal))
        {
            state.Data[SegmentPhaseDataKey] = PhaseAwaitingUdxMapReady;
            state.Data.Remove(UdxMapDataReadyDataKey);
            state.Data.Remove(UdxMapEnterAcknowledgedDataKey);
            state.Step = PhaseAwaitingUdxMapReady;
            logger.LogInformation("Transferring character {CharacterId} to Akihabara UDX ({DestinationMapId}) before {ClientScriptKey}", session.CharacterId, AkihabaraUdxMapId, ScriptedEvents.Keys.Bat0101021);
            if (!await directMapLinkTransitionService.TryTeleportToMapAsync(session, AkihabaraUdxMapId, ct))
            {
                logger.LogWarning("Aborting server script {EventKey} for character {CharacterId}: teleport to map {DestinationMapId} failed", EventKey, session.CharacterId, AkihabaraUdxMapId);
                await serverScriptSession.AbortAsync(session, EventFailure, ct);
            }

            return true;
        }

        if (string.Equals(phase, PhaseTpsBat0101021, StringComparison.Ordinal))
        {
            var returnMapId = state.Data.TryGetValue(ReturnMyRoomMapDataKey, out var rawReturnMapId) && rawReturnMapId is uint storedMapId && MyRoomInfo.IsMyRoomMap(storedMapId) ? storedMapId : MyRoomInfo.BaseMapId;
            var returnRoomId = state.Data.TryGetValue(ReturnMyRoomIdDataKey, out var rawReturnRoomId) && rawReturnRoomId is uint storedRoomId && storedRoomId <= int.MaxValue ? checked((int)storedRoomId) : 0;
            await serverScriptSession.CompleteAsync(session, 0, markComplete: true, ct);
            var returnRoom = returnRoomId == 0 ? null : await myRoomRepository.GetRoomAsync(returnRoomId, ct);
            var returned = returnRoom is not null ? await directMapLinkTransitionService.TryTeleportToRoomAsync(session, returnRoom, ct) : await directMapLinkTransitionService.TryTeleportToMapAsync(session, returnMapId, ct);
            if (!returned)
                logger.LogWarning("Server script {EventKey} completed for character {CharacterId}, but teleport back to MyRoom map {DestinationMapId} failed", EventKey, session.CharacterId, returnMapId);
            return true;
        }

        logger.LogWarning("Aborting server script {EventKey} for character {CharacterId}: unexpected phase {Phase}", EventKey, session.CharacterId, phase);
        await serverScriptSession.AbortAsync(session, EventFailure, ct);
        return true;
    }

    private async Task<bool> TryResumeAfterUdxMapReadyAsync(PacketType packetType, IPlayerSession session, ServerScriptState state, CancellationToken ct)
    {
        if (session.MapId != AkihabaraUdxMapId)
            return false;

        if (packetType == PacketType.MapDataEnterEndRequest)
            state.Data[UdxMapDataReadyDataKey] = true;
        else
            state.Data[UdxMapEnterAcknowledgedDataKey] = true;

        var mapDataReady = state.Data.TryGetValue(UdxMapDataReadyDataKey, out var rawMapDataReady) && rawMapDataReady is true;
        var mapEnterAcknowledged = state.Data.TryGetValue(UdxMapEnterAcknowledgedDataKey, out var rawMapEnterAcknowledged) && rawMapEnterAcknowledged is true;
        if (session.IsMapTransitionPending || !mapDataReady || !mapEnterAcknowledged)
        {
            logger.LogDebug("Waiting to start {ClientScriptKey} for character {CharacterId}: mapDataReady={MapDataReady}, mapEnterAcknowledged={MapEnterAcknowledged}, transitionPending={TransitionPending}", ScriptedEvents.Keys.Bat0101021, session.CharacterId, mapDataReady, mapEnterAcknowledged, session.IsMapTransitionPending);
            return true;
        }

        state.Data[SegmentPhaseDataKey] = PhaseTpsBat0101021;
        logger.LogInformation("Restarting the client event and starting script segment {ClientScriptKey} for character {CharacterId} after Akihabara UDX map load and MapEnter acknowledgement", ScriptedEvents.Keys.Bat0101021, session.CharacterId);
        await session.SendAsync(PacketType.EventStartNotify, new EventStartNotify().ToBytes(), ct);
        await clientScriptSegmentRunner.BeginAsync(session, ScriptedEvents.Keys.Bat0101021, ct);
        return true;
    }

    private async Task<bool> HandleCompletedDoorSelectionAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct)
    {
        var request = EventSelectExecRRequest.FromBytes(payload.Span);
        if (request.Result != 0 || request.SelectNo == 1)
        {
            await serverScriptSession.CompleteAsync(session, request.Result, markComplete: false, ct);
            return true;
        }

        if (request.SelectNo != 0)
        {
            await serverScriptSession.AbortAsync(session, EventFailure, ct);
            return true;
        }

        var character = await directMapLinkTransitionService.ResolveCharacterAsync(session, ct);
        if (character is null || !ShoppingAreaMapIds.TryGetValue(character.HomeIslandId, out var destinationMapId))
        {
            logger.LogWarning("Cannot return character {CharacterId} from MyRoom: unsupported home island {IslandId}", session.CharacterId, character?.HomeIslandId);
            await serverScriptSession.AbortAsync(session, EventFailure, ct);
            return true;
        }

        await serverScriptSession.CompleteAsync(session, 0, markComplete: false, ct);
        if (!await directMapLinkTransitionService.TryTeleportToMapAsync(session, destinationMapId, ct))
            logger.LogWarning("Completed MyRoom door selection for character {CharacterId}, but teleport to shopping area map {DestinationMapId} failed", session.CharacterId, destinationMapId);
        return true;
    }
}
