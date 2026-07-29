using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaRoomListCloseHandler : PacketHandlerBase<RoomListCloseRequest, RoomListCloseResponse>, IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.RoomListCloseRequest;
    public override PacketType ResponseType => PacketType.RoomListCloseResponse;
    public override ServerType ServerType => ServerType.Area;

    public override Task<RoomListCloseResponse?> HandleAsync(RoomListCloseRequest request, IPlayerSession session, CancellationToken ct = default) => Task.FromResult<RoomListCloseResponse?>(new RoomListCloseResponse(0));
}
