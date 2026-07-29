using AISpace.Network;

namespace AISpace.Common.Handlers.Common;

public class MsgVersionCheckHandler : VersionCheckHandlerBase
{
    public override ServerType ServerType => ServerType.Msg;
}
