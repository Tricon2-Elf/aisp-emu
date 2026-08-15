using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class ItemEquipStartHandler(
    IRoboRepository roboRepository,
    ILogger<ItemEquipStartHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.ItemEquipStartRequest;

    public PacketType ResponseType => PacketType.ItemEquipStartResponse;

    public ServerType ServerType => ServerType.Area;

    private readonly ILogger<ItemEquipStartHandler> _logger = logger;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = ItemEquipStartRequest.FromBytes(payload.Span);
        _logger.LogInformation(
            "Client {Id} requested ItemEquipStart for ObjId: {ObjId}",
            session.ConnectionId,
            request.ObjId
        );

        var ownsTarget = session.CharacterId != 0 && request.ObjId == session.CharacterId;
        if (
            !ownsTarget
            && session.CharacterId != 0
            && RoboRepository.TryGetRoboId(session.CharacterId, request.ObjId, out var roboId)
        )
            ownsTarget = await roboRepository.ExistsAsync(
                checked((int)session.CharacterId),
                roboId,
                ct
            );

        if (!ownsTarget)
        {
            await session.SendAsync(ResponseType, new ItemEquipStartResponse(0).ToBytes(), ct);
            return;
        }

        // Let the client advance to wardrobe state 3 after send_item_equip_start before acks arrive.
        await Task.Delay(100, ct);

        // recv_item_equip_start_r → sub_529E80: result==0 closes the UI; non-zero is a no-op on that path.
        // Empirically result=1 + force_started opens the wardrobe; result=0 freezes or only plays the curtain.
        await session.SendAsync(ResponseType, new ItemEquipStartResponse(1).ToBytes(), ct);

        // Protocol also lists equip_started; send both started packets after start_r.
        await session.SendAsync(
            PacketType.ItemEquipStarted,
            new ItemEquipStarted(request.ObjId).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.ItemEquipForceStarted,
            new ItemEquipForceStarted(request.ObjId).ToBytes(),
            ct
        );
    }
}
