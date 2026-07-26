using AISpace.Common.DAL.Entities;
using AISpace.Common.Game.ServerScripts;
using AISpace.Network;

namespace AISpace.Common.Game;

/// <summary>
/// In-memory game session used by Common when no network adapter is present (e.g. tests).
/// Holds mutable state and delegates send to an IMessageSender.
/// </summary>
public class PlayerSession : IPlayerSession
{
    public PlayerSession(Guid connectionId, ClientConnection clientConnection)
    {
        ConnectionId = connectionId;
        this.ClientConnection = clientConnection;
    }

    public ClientConnection ClientConnection;
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
    public int Rotation { get; set; }
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
    public ISet<uint> AccompanyingRoboIds { get; } = new HashSet<uint>();
    public ISet<uint> VisibleRemoteRoboObjectIds { get; } = new HashSet<uint>();
    public bool IsAuthenticated => User != null;

    public Task SendAsync(PacketType type, byte[] payload, CancellationToken ct = default) => ClientConnection.SendAsync(type, payload, ct);
}
