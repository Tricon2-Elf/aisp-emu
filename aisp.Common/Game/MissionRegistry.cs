using System.Collections.Concurrent;

namespace aisp.Common.Game;

/// <summary>Live missions. Handlers resolve the one a player is in and edit that instance.</summary>
public sealed class MissionRegistry
{
    private readonly ConcurrentDictionary<Guid, MissionInstance> _byId = new();
    private readonly ConcurrentDictionary<uint, Guid> _byCharacter = new();
    private readonly ConcurrentDictionary<Guid, Guid> _byConnection = new();

    public MissionInstance Create(IPlayerSession session)
    {
        if (TryGet(session) is { } existing)
            return existing;

        var mission = new MissionInstance(Guid.NewGuid(), session.MapId, session.CharacterId);
        mission.TryAddMember(session);
        _byId[mission.Id] = mission;
        _byCharacter[session.CharacterId] = mission.Id;
        _byConnection[session.ConnectionId] = mission.Id;
        return mission;
    }

    public MissionInstance? TryGet(IPlayerSession session)
    {
        if (
            session.ActiveMissionId is Guid id
            && _byId.TryGetValue(id, out var mission)
            && mission.FindMember(session.CharacterId) is not null
        )
            return mission;

        if (
            _byCharacter.TryGetValue(session.CharacterId, out id)
            && _byId.TryGetValue(id, out mission)
        )
            return mission;

        return null;
    }

    public bool TryAddMember(MissionInstance mission, IPlayerSession session)
    {
        if (!mission.TryAddMember(session))
            return false;

        _byId[mission.Id] = mission;
        _byCharacter[session.CharacterId] = mission.Id;
        _byConnection[session.ConnectionId] = mission.Id;
        return true;
    }

    public void RemoveMember(IPlayerSession session)
    {
        var mission = TryGet(session);
        session.ActiveMissionId = null;
        session.IsTpsMode = false;
        TpsMissionGameOver.CancelPendingClear(session.ConnectionId);
        if (mission is null)
            return;

        _byCharacter.TryRemove(session.CharacterId, out _);
        _byConnection.TryRemove(session.ConnectionId, out _);
        if (mission.RemoveMember(session.CharacterId))
            Drop(mission);
    }

    public void RemoveByConnection(Guid connectionId)
    {
        if (!_byConnection.TryGetValue(connectionId, out var missionId))
            return;
        if (!_byId.TryGetValue(missionId, out var mission))
        {
            _byConnection.TryRemove(connectionId, out _);
            return;
        }

        var member = mission.Members.FirstOrDefault(candidate =>
            candidate.ConnectionId == connectionId
        );
        if (member is null)
        {
            _byConnection.TryRemove(connectionId, out _);
            return;
        }

        RemoveMember(member.Session);
    }

    private void Drop(MissionInstance mission)
    {
        mission.StopLoop();
        _byId.TryRemove(mission.Id, out _);
    }
}
