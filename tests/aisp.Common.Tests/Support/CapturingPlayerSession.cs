using aisp.Common.DAL.Entities;
using aisp.Common.Game;
using aisp.Common.Game.ServerScripts;
using aisp.Common.Localisation;
using aisp.Network;

namespace aisp.Common.Tests.Support;

internal sealed class CapturingPlayerSession : IPlayerSession
{
    public CapturingPlayerSession(Guid? connectionId = null) =>
        ConnectionId = connectionId ?? Guid.NewGuid();

    public Guid ConnectionId { get; }
    public int UserId { get; set; }
    public uint CharacterId { get; set; }
    public Character? Character { get; set; }
    public User? User { get; set; }
    public GameLanguage Language { get; set; } = GameLanguage.English;
    public uint MapId { get; set; }
    public uint MyRoomId { get; set; }
    public uint? PendingMyRoomFurnitureItemId { get; set; }
    public StorageOpenContext StorageOpenContext { get; set; }
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
    public uint AdventureReturnMapId { get; set; }
    public string? ActiveEventKey { get; set; }
    public NpcEventKind ActiveEventKind { get; set; }
    public EventCompletionPolicy ActiveEventCompletionPolicy { get; set; }
    public ServerScriptState? ServerScriptState { get; set; }
    public ISet<uint> AccompanyingRoboIds { get; } = new HashSet<uint>();
    public ISet<uint> VisibleRemoteRoboObjectIds { get; } = new HashSet<uint>();
    public bool IsAuthenticated => User != null;

    public bool HangOnSend { get; set; }

    public List<(PacketType Type, byte[] Payload)> Sent { get; } = new();

    public async Task SendAsync(PacketType type, byte[] payload, CancellationToken ct = default)
    {
        Sent.Add((type, payload));
        if (!HangOnSend)
            return;

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        }
        catch (OperationCanceledException)
        {
            // test shutdown
        }
    }
}
