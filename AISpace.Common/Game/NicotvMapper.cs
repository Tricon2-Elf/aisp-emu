using AISpace.Common.DAL.Entities;
using AISpace.Network.Data;

namespace AISpace.Common.Game;

internal static class NicotvMapper
{
    public static NicotvData ToPacket(Nicotv nicotv) =>
        new(nicotv.ChannelId, nicotv.MovieId, nicotv.PlaybackState, nicotv.CommentVisibility);
}
