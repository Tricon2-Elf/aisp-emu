using AISpace.Common;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Server;

public class AreaServer(ILogger<AreaServer> logger, GameServerContext ctx, int port) : GameServerBase<AreaServer>(logger, ctx, port)
{
    protected override ServerType ActiveServerType => ServerType.Area;

    protected override TimeSpan? GameLoopInterval => TimeSpan.FromSeconds(1);

    protected override void OnTick(CancellationToken ct) => BroadcastServerTime();

    private void BroadcastServerTime()
    {
        var t = TimeZoneService.GetServerTime();

        var timePacket = new TimeZoneGetResponse(0, (uint)t.Phase, t.Current, t.Max, 0);
        byte[] data = timePacket.ToBytes();

        foreach (var client in State.GetServerClients(ServerType.Area))
        {
            if (client.IsAuthenticated)
                _ = client.SendAsync(PacketType.TimeZoneGetResponse, data);
        }
    }
}
