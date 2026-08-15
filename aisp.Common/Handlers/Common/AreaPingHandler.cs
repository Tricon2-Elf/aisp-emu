using aisp.Network;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Common;

public class AreaPingHandler(ILogger<AreaPingHandler> logger) : PingHandlerBase(logger)
{
    public override ServerType ServerType => ServerType.Area;
}
