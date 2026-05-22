using AISpace.Common;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Server;

public class AreaServer(ILogger<AreaServer> logger, GameServerContext ctx, int port) : GameServerBase<AreaServer>(logger, ctx, port)
{
    protected override ServerType ActiveServerType => ServerType.Area;
    private DateTime _nextTimeUpdate = DateTime.MinValue;

    protected override void OnTick(CancellationToken ct) => UpdateWorld();

    private void UpdateWorld()
    {
        if (DateTime.UtcNow > _nextTimeUpdate)
        {
            var t = TimeZoneService.GetServerTime();

            var timePacket = new TimeZoneGetResponse(0, (uint)t.Phase, t.Current, t.Max, 0);
            byte[] data = timePacket.ToBytes();

            foreach (var client in State.GetServerClients(ServerType.Area))
            {
                if (client.IsAuthenticated)
                    _ = client.SendAsync(PacketType.TimeZoneGetResponse, data);
            }

            _nextTimeUpdate = DateTime.UtcNow.AddSeconds(1);
        }
    }
}
