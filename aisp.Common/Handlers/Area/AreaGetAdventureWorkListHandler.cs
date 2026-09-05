using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>Registry of the account's drama works; the client merges it by work id with its local work/drama/list.csv.</summary>
public sealed class AreaGetAdventureWorkListHandler(IAdventureWorkRepository works)
    : PacketHandlerBase<GetAdventureWorkListRequest, GetAdventureWorkListResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.GetAdventureWorkListRequest;
    public override PacketType ResponseType => PacketType.GetAdventureWorkListResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<GetAdventureWorkListResponse?> HandleAsync(
        GetAdventureWorkListRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var (sheetStock, list) = await works.GetWorksAsync(session.User?.Id ?? session.UserId, ct);
        var records = list.Select(w => ((uint)w.WorkId, (uint)w.Sheets, w.Uploaded)).ToList();
        return new GetAdventureWorkListResponse(0, (uint)sheetStock, records);
    }
}
