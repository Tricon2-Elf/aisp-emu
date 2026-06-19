using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Msg;

public class AvatarCreateHandler(ILogger<AvatarCreateHandler> logger, ICharacterRepository charRepo) : PacketHandlerBase<AvatarCreateRequest, AvatarCreateResponse>, IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.AvatarCreateRequest;
    public override PacketType ResponseType => PacketType.AvatarCreateResponse;
    public override ServerType ServerType => ServerType.Msg;

    private readonly ILogger<AvatarCreateHandler> _logger = logger;

    public override async Task<AvatarCreateResponse?> HandleAsync(AvatarCreateRequest request, IPlayerSession session, CancellationToken ct = default)
    {
        _logger.LogInformation("createRequest: {request}", request.ToString());

        Character newChar = await charRepo.CreateAsync(request.AvatarName, session.User!.Id, request.modelId, request.visual.BloodType, request.visual.Birthdate, (int)request.visual.Gender, request.visual.Face, request.visual.Hairstyle, ct);

        if ((int)request.visual.Gender == 1)
        {
            await charRepo.EquipAsync(newChar.Id, 0, 10100220, ct);
            await charRepo.EquipAsync(newChar.Id, 1, 10200100, ct);
            await charRepo.EquipAsync(newChar.Id, 2, 10400030, ct);
            await charRepo.EquipAsync(newChar.Id, 3, 10500070, ct);
            // Wardrobe preview curtain (sub_4013E0) requires 0x800 on an equipped item for males.
            await charRepo.EquipAsync(newChar.Id, 4, 10700020, ct);
        }
        else
        {
            await charRepo.EquipAsync(newChar.Id, 0, 10100060, ct);
            await charRepo.EquipAsync(newChar.Id, 1, 10200090, ct);
            await charRepo.EquipAsync(newChar.Id, 2, 10400000, ct);
            await charRepo.EquipAsync(newChar.Id, 3, 10500010, ct);
        }

        return new AvatarCreateResponse(0);
    }
}
