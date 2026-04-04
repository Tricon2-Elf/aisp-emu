using AISpace.Common.DAL.Entities;
using AISpace.Network;

namespace AISpace.Common.Game;

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
    int ChannelId { get; set; }
    float X { get; set; }
    float Y { get; set; }
    float Z { get; set; }
    sbyte Rotation { get; set; }
    int MovementTypeId { get; set; }
    bool HasMovedSinceMapLoad { get; set; }
    bool IsMapTransitionPending { get; set; }
    bool NeedsPostLoadSelfAvatarNotify { get; set; }
    PendingAreaMapSelection? PendingAreaMapSelection { get; set; }
    bool IsAuthenticated { get; }

    Task SendAsync(PacketType type, byte[] payload, CancellationToken ct = default);
}
