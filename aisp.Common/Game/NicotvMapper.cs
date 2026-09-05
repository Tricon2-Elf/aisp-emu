using System.Globalization;
using aisp.Common.DAL.Entities;
using aisp.Network.Data;

namespace aisp.Common.Game;

internal static class NicotvMapper
{
    public static NicotvData ToPacket(Nicotv nicotv) =>
        new(
            nicotv.ChannelId,
            WithNicotvId(nicotv.MovieId, checked((uint)nicotv.Id)),
            nicotv.PlaybackState,
            nicotv.CommentVisibility
        );

    /// <summary>
    /// Appends the furniture's own database id as an n: tag (movieId's whole content when it is
    /// empty), so wherever this reaches the screen page's movieid= it round-trips: the server
    /// then keys the shared timeline, and resolves content, by this specific TV rather than by
    /// map and channel, which do not reliably tell one player's room from another's. Short (n:,
    /// not nicotvid:) since it is invisible wire budget inside a 96-character movie id, not
    /// something read by a person. Not tvid:, which is the client's own word for a channel number
    /// (see ScreenAssignments.IsChannelSource), an unrelated thing. Falls back to the plain movie
    /// id on an implausibly long typed string rather than exceed that limit.
    /// </summary>
    public static string WithNicotvId(string movieId, uint nicotvId)
    {
        var tag = "n:" + nicotvId.ToString(CultureInfo.InvariantCulture);
        var tagged = movieId.Length == 0 ? tag : $"{movieId} {tag}";
        return tagged.Length <= NicotvData.MovieIdLength - 1 ? tagged : movieId;
    }
}
