using System.Linq;
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

    public Task<BroadcastResult> BroadcastAsync(string message, CancellationToken ct = default) =>
        BroadcastToServersAsync(message, [ServerType.Area, ServerType.Msg], ct);

    public async Task<BroadcastResult> BroadcastToServersAsync(
        string message,
        IReadOnlyList<ServerType> serverTypes,
        CancellationToken ct = default
    )
    {
        var forward = new TalkForwardNotify(0, 0, message, 0);
        var data = forward.ToBytes();

        int area = 0,
            msg = 0;

        foreach (var serverType in serverTypes.Distinct())
        {
            foreach (var client in _state.GetServerClients(serverType))
            {
                if (client.IsAuthenticated)
                {
                    await client.SendAsync(PacketType.TalkForwardNotify, data, ct);
                    if (serverType == ServerType.Area)
                        area++;
                    else if (serverType == ServerType.Msg)
                        msg++;
                }
            }
        }

        return new BroadcastResult(area, msg);
    }
}

public record BroadcastResult(int AreaClients, int MsgClients);
