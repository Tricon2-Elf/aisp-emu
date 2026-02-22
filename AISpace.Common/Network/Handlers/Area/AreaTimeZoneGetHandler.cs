using AISpace.Common.Game;
using AISpace.Common.Network.Packets;

namespace AISpace.Common.Network.Handlers;

public class AreaTimeZoneGetHandler(SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.TimeZoneGetRequest;
    public PacketType ResponseType => PacketType.TimeZoneGetResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        uint[] durations = { 900, 1800, 3600, 900, 1800 };
        uint[] indices = { 4, 0, 1, 2, 3 };
        
        long totalCycle = 9000;
        long elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - state.StartTimeUnix;
        uint cyclePos = (uint)(elapsed % totalCycle);

        uint timezone = 4;
        uint timeInPeriod = cyclePos;
        uint maxInPeriod = durations[0];

        uint accumulated = 0;
        for (int i = 0; i < durations.Length; i++)
        {
            if (cyclePos < accumulated + durations[i])
            {
                timezone = indices[i];
                timeInPeriod = cyclePos - accumulated;
                maxInPeriod = durations[i];
                break;
            }
            accumulated += durations[i];
        }

        var response = new TimeZoneGetResponse(0, timezone, timeInPeriod, maxInPeriod, 0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
