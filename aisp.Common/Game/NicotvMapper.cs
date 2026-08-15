using aisp.Common.DAL.Entities;
using aisp.Network.Data;

namespace aisp.Common.Game;

internal static class NicotvMapper
{
    public static NicotvData ToPacket(Nicotv nicotv) =>
        new(nicotv.ChannelId, nicotv.MovieId, nicotv.PlaybackState, nicotv.CommentVisibility);
}
