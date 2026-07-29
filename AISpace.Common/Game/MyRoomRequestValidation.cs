using AISpace.Common.DAL.Repositories;
using AISpace.Network;

namespace AISpace.Common.Game;

internal static class MyRoomRequestValidation
{
    public static async Task<bool> IsOwnerInRoomAsync(uint roomId, IPlayerSession session, IMyRoomRepository myRoomRepository, CancellationToken ct)
    {
        if (session.CharacterId == 0 || roomId == 0 || roomId != session.MyRoomId || !MyRoomInfo.IsMyRoomMap(session.MapId) || roomId > int.MaxValue)
            return false;

        return await myRoomRepository.IsOwnerAsync(checked((int)roomId), checked((int)session.CharacterId), ct);
    }
}
