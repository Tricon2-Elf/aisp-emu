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
            for (byte slot = 0; slot < 4; slot++)
                await charRepo.EquipAsync(newChar.Id, slot, DefaultClothingItems.Male[slot], ct);
        else
            for (byte slot = 0; slot < 4; slot++)
                await charRepo.EquipAsync(newChar.Id, slot, DefaultClothingItems.Female[slot], ct);

        foreach (var itemId in DefaultClothingItems.WardrobeInventoryForGender((int)request.visual.Gender))
            await charRepo.AddInventoryAsync(newChar.Id, itemId, 1, ct);

        return new AvatarCreateResponse(0);
    }
}
