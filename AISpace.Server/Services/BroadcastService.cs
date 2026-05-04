using AISpace.Common;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Msg;

namespace AISpace.Server.Services;

public class BroadcastService
{
    private readonly SharedState _state;

    public BroadcastService(SharedState state)
    {
        _state = state;
    }

    public async Task<BroadcastResult> BroadcastAsync(string message, CancellationToken ct = default)
    {
        var forward = new TalkForwardNotify(0, 0, message, 0);
        var data = forward.ToBytes();

        int area = 0, msg = 0;

        foreach (var client in _state.GetServerClients(ServerType.Area))
        {
            if (client.IsAuthenticated)
            {
                await client.SendAsync(PacketType.TalkForwardNotify, data, ct);
                area++;
            }
        }

        foreach (var client in _state.GetServerClients(ServerType.Msg))
        {
            if (client.IsAuthenticated)
            {
                await client.SendAsync(PacketType.TalkForwardNotify, data, ct);
                msg++;
            }
        }

        return new BroadcastResult(area, msg);
    }
}

public record BroadcastResult(int AreaClients, int MsgClients);
