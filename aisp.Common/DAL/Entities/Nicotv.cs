using aisp.Network;
using aisp.Network.Data;

namespace aisp.Common.DAL.Entities;

public sealed class Nicotv
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public uint FurnitureId { get; set; }
    public uint ChannelId { get; set; }
    public string MovieId { get; set; } = "";
    public NicotvPlaybackState PlaybackState { get; set; } = NicotvPlaybackState.Closed;

    // The server's own default for the TV's comment overlay, not the client's: the client has no
    // request for it and its TV panel sends every snapshot as comments visible. Off unless a
    // moderator turns it on for a TV. The page reads it from its title when served.
    public NicotvCommentVisibility CommentVisibility { get; set; } = NicotvCommentVisibility.Hidden;
    public DateTime UpdatedAt { get; set; }

    public MyRoomFurniture Furniture { get; set; } = default!;
}
