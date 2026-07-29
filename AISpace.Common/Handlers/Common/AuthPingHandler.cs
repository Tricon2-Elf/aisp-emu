using AISpace.Network;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Common;

public class AuthPingHandler(ILogger<AuthPingHandler> logger) : PingHandlerBase(logger)
{
    public override ServerType ServerType => ServerType.Auth;
}
