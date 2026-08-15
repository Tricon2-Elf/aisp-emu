using aisp.Network;

namespace aisp.Common.Handlers.Common;

public class MsgVersionCheckHandler : VersionCheckHandlerBase
{
    public override ServerType ServerType => ServerType.Msg;
}
