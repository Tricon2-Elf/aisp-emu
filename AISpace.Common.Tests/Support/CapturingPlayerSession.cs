using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using AISpace.Common.Game.ServerScripts;
using AISpace.Network;

namespace AISpace.Common.Tests.Support;

internal sealed class CapturingPlayerSession : IPlayerSession
{
    public CapturingPlayerSession(Guid? connectionId = null) => ConnectionId = connectionId ?? Guid.NewGuid();

    public Guid ConnectionId { get; }
    public int UserId { get; set; }
    public uint CharacterId { get; set; }
    public Character? Character { get; set; }
    public User? User { get; set; }
    public uint MapId { get; set; }
    public int ChannelId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public sbyte Rotation { get; set; }
    public int MovementTypeId { get; set; }
    public bool HasMovedSinceMapLoad { get; set; }
    public bool IsMapTransitionPending { get; set; }
    public bool NeedsPostLoadSelfAvatarNotify { get; set; }
    public PendingAreaMapSelection? PendingAreaMapSelection { get; set; }
    public int? ActiveShopId { get; set; }
    public bool PendingEventEndAfterFade { get; set; }
    public string? ActiveEventKey { get; set; }
    public NpcEventKind ActiveEventKind { get; set; }
    public EventCompletionPolicy ActiveEventCompletionPolicy { get; set; }
    public ServerScriptState? ServerScriptState { get; set; }
    public bool IsAuthenticated => User != null;

    public List<(PacketType Type, byte[] Payload)> Sent { get; } = new();

    public Task SendAsync(PacketType type, byte[] payload, CancellationToken ct = default)
    {
        Sent.Add((type, payload));
        return Task.CompletedTask;
    }
}
