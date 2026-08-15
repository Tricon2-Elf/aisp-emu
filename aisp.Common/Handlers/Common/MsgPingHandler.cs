using aisp.Network;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Common;

public class MsgPingHandler(ILogger<MsgPingHandler> logger) : PingHandlerBase(logger)
{
    public override ServerType ServerType => ServerType.Msg;
}
