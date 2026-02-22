using AISpace.Common.DAL.Repositories;
using AISpace.Common.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Msg;

public class AvatarCreateHandler(ICharacterRepository charRepo, ILogger<AvatarCreateHandler> logger) : PacketHandlerBase<AvatarCreateRequest, AvatarCreateResponse>
{
    public override PacketType RequestType => PacketType.AvatarCreateRequest;
    public override PacketType ResponseType => PacketType.AvatarCreateResponse;
    public override MessageDomain Domain => MessageDomain.Msg;

    public override async Task<AvatarCreateResponse?> HandleAsync(AvatarCreateRequest request, ClientConnection connection, CancellationToken ct = default) {
        if (connection.User == null) return null;
        await charRepo.CreateAsync(request.AvatarName, connection.User.Id, request.modelId, request.visual.BloodType, request.visual.Birthdate, (int)request.visual.Gender, request.visual.Face, request.visual.Hairstyle, ct);
        return new AvatarCreateResponse(0);
    }
}