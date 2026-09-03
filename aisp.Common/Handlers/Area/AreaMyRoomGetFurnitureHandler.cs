using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaMyRoomGetFurnitureHandler(
    IRoboRepository roboRepository,
    IMyRoomRepository myRoomRepository,
    SharedState state
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MyRoomGetFurnitureRequest;

    public PacketType ResponseType => PacketType.MyRoomGetFurnitureResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = MyRoomGetFurnitureRequest.FromBytes(payload.Span);
        if (request.MapId != session.MapId || request.ChannelId != checked((uint)session.ChannelId))
        {
            await session.SendAsync(ResponseType, new MyRoomGetFurnitureResponse(1).ToBytes(), ct);
            return;
        }

        var packets = new List<(PacketType Type, byte[] Payload)>();

        if (MyRoomInfo.IsMyRoomMap(session.MapId))
        {
            if (session.MyRoomId == 0)
            {
                await session.SendAsync(
                    ResponseType,
                    new MyRoomGetFurnitureResponse(1).ToBytes(),
                    ct
                );
                return;
            }

            var room = await myRoomRepository.GetRoomAsync(checked((int)session.MyRoomId), ct);
            if (room is null)
            {
                await session.SendAsync(
                    ResponseType,
                    new MyRoomGetFurnitureResponse(1).ToBytes(),
                    ct
                );
                return;
            }

            var furniture = await myRoomRepository.GetFurnitureAsync(room.Id, ct);
            foreach (var placement in furniture)
            {
                packets.Add(
                    (
                        PacketType.MyRoomNotifyFurniture,
                        new MyRoomNotifyFurniture(
                            MyRoomFurnitureMapper.ToPacket(placement)
                        ).ToBytes()
                    )
                );
            }

            if (room.OwnerCharacterId == checked((int)session.CharacterId))
            {
                var availableFurniture = await myRoomRepository.GetAvailableFurnitureInventoryAsync(
                    room.OwnerCharacterId,
                    ct
                );
                foreach (var stack in availableFurniture.OrderBy(x => x.Key))
                {
                    CharacterItemSync.AppendFurnitureInventoryAvailability(
                        packets,
                        stack.Key,
                        stack.Value
                    );
                }

                var robos = await roboRepository.GetAllAsync(room.OwnerCharacterId, ct);
                foreach (var robo in robos)
                {
                    session.AccompanyingRoboIds.Remove(robo.RoboId);

                    var map = new CharacterMapData
                    {
                        ChannelId = checked((uint)session.ChannelId),
                        MapId = session.MapId,
                        Movement = new MovementData(
                            session.X,
                            session.Y,
                            session.Z - 50f,
                            session.Rotation,
                            MovementType.Stopped
                        ),
                    };
                    var notify = new NotifyUpdateRoboState(
                        robo.RoboId,
                        robo.Character.SlotId,
                        (uint)RoboState.InMyRoom,
                        map
                    );
                    state.RememberRoboMovement(session.CharacterId, robo.RoboId, map.Movement);
                    packets.Add((PacketType.NotifyUpdateRoboState, notify.ToBytes()));
                }
            }
        }

        // Keep the client in its furniture-loading wait state until Robo activation has been queued.
        packets.Add((ResponseType, new MyRoomGetFurnitureResponse(0).ToBytes()));
        await session.SendAsync(packets, ct);
    }
}
