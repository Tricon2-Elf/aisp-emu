using AISpace.Common.DAL.Repositories;
using AISpace.Common.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Msg;

public class AvatarCreateHandler(ICharacterRepository charRepo, ILogger<AvatarCreateHandler> logger) : PacketHandlerBase<AvatarCreateRequest, AvatarCreateResponse>
{
    public override PacketType RequestType => PacketType.AvatarCreateRequest;
    public override PacketType ResponseType => PacketType.AvatarCreateResponse;
    public override MessageDomain Domain => MessageDomain.Msg;

    public override async Task<AvatarCreateResponse?> HandleAsync(AvatarCreateRequest request, ClientConnection connection, CancellationToken ct = default) 
    {
        if (connection.User == null) return null;

        var character = await charRepo.CreateAsync(
            request.AvatarName, 
            connection.User.Id, 
            request.modelId,
            request.visual.BloodType, 
            request.visual.Birthdate, 
            (int)request.visual.Gender, 
            request.visual.Face, 
            request.visual.Hairstyle, 
            ct);

        bool isFemale = request.visual.Gender == 2;

        if (isFemale)
        {
            logger.LogInformation($"[CREATE] Equipping female default set for '{character.Name}'");
            await charRepo.EquipAsync(character.Id, 0, 10100060, ct); //майка
            await charRepo.EquipAsync(character.Id, 1, 10200090, ct); //шорты
            await charRepo.EquipAsync(character.Id, 4, 10400000, ct); //носки
            await charRepo.EquipAsync(character.Id, 5, 10500010, ct); //обувь
        }
        else // мужчина
        {
            logger.LogInformation($"[CREATE] Equipping male default set for '{character.Name}'");
            await charRepo.EquipAsync(character.Id, 0, 10100220, ct); //майка
            await charRepo.EquipAsync(character.Id, 1, 10200100, ct); //штОны
            await charRepo.EquipAsync(character.Id, 4, 10400030, ct); //носки
            await charRepo.EquipAsync(character.Id, 5, 10500070, ct); //обувь
        }
        
        return new AvatarCreateResponse(0);
    }
}
