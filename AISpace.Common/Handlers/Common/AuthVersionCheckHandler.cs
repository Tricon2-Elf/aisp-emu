using AISpace.Network;

namespace AISpace.Common.Handlers.Common;

public class AuthVersionCheckHandler : VersionCheckHandlerBase
{
    public override ServerType ServerType => ServerType.Auth;
}
