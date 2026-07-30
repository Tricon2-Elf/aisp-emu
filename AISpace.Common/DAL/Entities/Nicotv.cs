using AISpace.Network.Data;

namespace AISpace.Common.DAL.Entities;

public sealed class Nicotv
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public uint FurnitureId { get; set; }
    public uint ChannelId { get; set; }
    public string MovieId { get; set; } = "";
    public NicotvPlaybackState PlaybackState { get; set; } = NicotvPlaybackState.Closed;
    public NicotvCommentVisibility CommentVisibility { get; set; } =
        NicotvCommentVisibility.Visible;
    public DateTime UpdatedAt { get; set; }

    public MyRoomFurniture Furniture { get; set; } = default!;
}
