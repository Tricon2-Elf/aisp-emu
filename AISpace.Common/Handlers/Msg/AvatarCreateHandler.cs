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
            await charRepo.EquipAsync(newChar.Id, 0, DefaultClothingItems.Male[0], ct);
            await charRepo.EquipAsync(newChar.Id, 1, DefaultClothingItems.Male[1], ct);
            await charRepo.EquipAsync(newChar.Id, 2, DefaultClothingItems.Male[2], ct);
            await charRepo.EquipAsync(newChar.Id, 3, DefaultClothingItems.Male[3], ct);
            // Wardrobe preview curtain (sub_4013E0) requires 0x800 on an equipped item for males.
            await charRepo.EquipAsync(newChar.Id, 4, DefaultClothingItems.Male[4], ct);
        }
        else
        {
            await charRepo.EquipAsync(newChar.Id, 0, DefaultClothingItems.Female[0], ct);
            await charRepo.EquipAsync(newChar.Id, 1, DefaultClothingItems.Female[1], ct);
            await charRepo.EquipAsync(newChar.Id, 2, DefaultClothingItems.Female[2], ct);
            await charRepo.EquipAsync(newChar.Id, 3, DefaultClothingItems.Female[3], ct);
        }

        foreach (var itemId in DefaultClothingItems.WardrobeInventoryForGender((int)request.visual.Gender))
            await charRepo.AddInventoryAsync(newChar.Id, itemId, 1, ct);

        return new AvatarCreateResponse(0);
    }
}
