using System.Numerics;
using aisp.Network.Data;

namespace aisp.Common.Game;

/// <summary>A Friend Link placard that exists only while its owner remains in the area.</summary>
public sealed record ActiveFriendLinkPlacard(
    uint PlacardId,
    int OwnerUserId,
    uint OwnerCharacterId,
    string OwnerName,
    uint MapId,
    int ChannelId,
    uint MyRoomId,
    uint TagId,
    uint Slot,
    byte Direction,
    string TagName,
    Vector3 Position
)
{
    private readonly object _commentLock = new();
    private readonly List<ActiveFriendLinkPlacardComment> _comments = [];

    public FriendLinkPlacardData ToPacketData() =>
        new(PlacardId, OwnerName, OwnerCharacterId, TagId, Slot, Direction, TagName, Position);

    public ActiveFriendLinkPlacardComment AddComment(
        int authorUserId,
        uint authorCharacterId,
        string authorName,
        string message
    )
    {
        var comment = new ActiveFriendLinkPlacardComment(
            authorUserId,
            authorCharacterId,
            authorName,
            message,
            DateTime.UtcNow
        );
        lock (_commentLock)
        {
            _comments.Add(comment);
            if (_comments.Count > 100)
                _comments.RemoveRange(0, _comments.Count - 100);
        }
        return comment;
    }

    public IReadOnlyList<ActiveFriendLinkPlacardComment> GetComments()
    {
        lock (_commentLock)
            return [.. _comments];
    }

    internal void ClearComments()
    {
        lock (_commentLock)
            _comments.Clear();
    }
}

public sealed record ActiveFriendLinkPlacardComment(
    int AuthorUserId,
    uint AuthorCharacterId,
    string AuthorName,
    string Message,
    DateTime CreatedAtUtc
);
