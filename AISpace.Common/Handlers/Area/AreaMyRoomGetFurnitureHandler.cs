using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaMyRoomGetFurnitureHandler(IRoboRepository roboRepository, IMyRoomRepository myRoomRepository) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MyRoomGetFurnitureRequest;

    public PacketType ResponseType => PacketType.MyRoomGetFurnitureResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = MyRoomGetFurnitureRequest.FromBytes(payload.Span);
        if (request.MapId != session.MapId || request.ChannelId != checked((uint)session.ChannelId))
        {
            await session.SendAsync(ResponseType, new MyRoomGetFurnitureResponse(1).ToBytes(), ct);
            return;
        }

        if (MyRoomInfo.IsMyRoomMap(session.MapId))
        {
            var furniture = await myRoomRepository.GetFurnitureAsync(checked((int)session.CharacterId), ct);
            foreach (var placement in furniture)
                await session.SendAsync(PacketType.MyRoomNotifyFurniture, new MyRoomNotifyFurniture(MyRoomFurnitureMapper.ToPacket(placement)).ToBytes(), ct);

            var robos = await roboRepository.GetAllAsync(checked((int)session.CharacterId), ct);
            foreach (var robo in robos)
            {
                session.AccompanyingRoboIds.Remove(robo.RoboId);
                var map = new CharacterMapData
                {
                    ChannelId = checked((uint)session.ChannelId),
                    MapId = session.MapId,
                    Movement = new MovementData(session.X, session.Y, session.Z - 50f, session.Rotation, MovementType.Stopped),
                };
                var notify = new NotifyUpdateRoboState(robo.RoboId, robo.Character.SlotId, (uint)RoboState.InMyRoom, map);
                await session.SendAsync(PacketType.NotifyUpdateRoboState, notify.ToBytes(), ct);
            }
        }

        // Keep the client in its furniture-loading wait state until Robo activation has been queued.
        var response = new MyRoomGetFurnitureResponse(0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
