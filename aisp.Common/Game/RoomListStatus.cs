namespace aisp.Common.Game;

/// <summary>
/// Presence value on <see cref="Network.Data.RoomListEntry.Status"/> (wire trailing dword).
/// Client Status-column sort order (help): At Home → Out → Away → Logged Out.
/// </summary>
/// <remarks>
/// Icon selection in <c>sub_565250</c> only special-cases <see cref="Out"/> (=1) as
/// お出かけ中 (PAS 82); other values use ログイン (PAS 81). PAS also defines ログオフ (83);
/// if the live client maps Logged Out to that unit, it is not visible in the Hex-Rays dump.
/// </remarks>
public static class RoomListStatus
{
    public const uint AtHome = 0;
    public const uint Out = 1;
    public const uint Away = 2;
    public const uint LoggedOut = 3;
}
