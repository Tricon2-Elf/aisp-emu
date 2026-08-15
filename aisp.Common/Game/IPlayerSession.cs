using aisp.Common.DAL.Entities;
using aisp.Common.Game.ServerScripts;
using aisp.Network;

namespace aisp.Common.Game;

/// <summary>
/// Represents a connected player session: identity, location/channel state, and ability to send packets.
/// Implemented by Common's PlayerSession or by the Server's ClientConnectionSessionAdapter.
/// </summary>
public interface IPlayerSession
{
    Guid ConnectionId { get; }
    int UserId { get; set; }
    uint CharacterId { get; set; }
    Character? Character { get; set; }
    User? User { get; set; }
    uint MapId { get; set; }
    uint MyRoomId { get; set; }
    uint? PendingMyRoomFurnitureItemId { get; set; }
    StorageOpenContext StorageOpenContext { get; set; }
    int ChannelId { get; set; }
    float X { get; set; }
    float Y { get; set; }
    float Z { get; set; }
    int Rotation { get; set; }
    int MovementTypeId { get; set; }
    bool HasMovedSinceMapLoad { get; set; }
    bool IsMapTransitionPending { get; set; }
    bool NeedsPostLoadSelfAvatarNotify { get; set; }
    PendingAreaMapSelection? PendingAreaMapSelection { get; set; }
    int? ActiveShopId { get; set; }
    bool PendingEventEndAfterFade { get; set; }
    string? ActiveEventKey { get; set; }
    NpcEventKind ActiveEventKind { get; set; }
    EventCompletionPolicy ActiveEventCompletionPolicy { get; set; }
    ServerScriptState? ServerScriptState { get; set; }
    ISet<uint> AccompanyingRoboIds { get; }
    ISet<uint> VisibleRemoteRoboObjectIds { get; }
    bool IsAuthenticated { get; }

    Task SendAsync(PacketType type, byte[] payload, CancellationToken ct = default);
}
