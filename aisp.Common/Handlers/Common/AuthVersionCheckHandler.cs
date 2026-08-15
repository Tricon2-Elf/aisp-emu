using aisp.Network;

namespace aisp.Common.Handlers.Common;

public class AuthVersionCheckHandler : VersionCheckHandlerBase
{
    public override ServerType ServerType => ServerType.Auth;
}
