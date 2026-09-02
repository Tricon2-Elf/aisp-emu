using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Msg;

public class AvatarCreateHandler(
    ILogger<AvatarCreateHandler> logger,
    ICharacterRepository charRepo,
    IWordFilter wordFilter
) : PacketHandlerBase<AvatarCreateRequest, AvatarCreateResponse>, IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.AvatarCreateRequest;
    public override PacketType ResponseType => PacketType.AvatarCreateResponse;
    public override ServerType ServerType => ServerType.Msg;

    private readonly ILogger<AvatarCreateHandler> _logger = logger;

    public override async Task<AvatarCreateResponse?> HandleAsync(
        AvatarCreateRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        _logger.LogInformation("createRequest: {request}", request.ToString());

        if (wordFilter.ContainsBlockedWord(request.AvatarName))
        {
            _logger.LogWarning(
                "Rejecting avatar create for user {UserId}: blocked name",
                session.User!.Id
            );
            return new AvatarCreateResponse(1);
        }

        var existing = await charRepo.GetByNameAsync(request.AvatarName, ct);
        if (existing is not null)
        {
            _logger.LogWarning(
                "Rejecting avatar create for user {UserId}: name '{Name}' already exists",
                session.User!.Id,
                request.AvatarName
            );
            return new AvatarCreateResponse(1);
        }

        Character newChar;
        try
        {
            newChar = await charRepo.CreateAsync(
                request.AvatarName,
                session.User!.Id,
                request.modelId,
                request.visual.BloodType,
                request.visual.Birthdate,
                (int)request.visual.Gender,
                request.visual.Face,
                request.visual.Hairstyle,
                ct
            );
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(
                ex,
                "Rejecting avatar create for user {UserId}: name '{Name}' already exists",
                session.User!.Id,
                request.AvatarName
            );
            return new AvatarCreateResponse(1);
        }

        if ((int)request.visual.Gender == 1)
            for (byte slot = 0; slot < 4; slot++)
                await charRepo.EquipAsync(newChar.Id, slot, DefaultClothingItems.Male[slot], ct);
        else
            for (byte slot = 0; slot < 4; slot++)
                await charRepo.EquipAsync(newChar.Id, slot, DefaultClothingItems.Female[slot], ct);

        foreach (
            var itemId in DefaultClothingItems.WardrobeInventoryForGender(
                (int)request.visual.Gender
            )
        )
            await charRepo.AddInventoryAsync(newChar.Id, itemId, 1, ct);

        // The authenticated Msg session was loaded before this character existed. Hydrate the
        // newly created character (including equipment) so this connection and later handlers
        // use the same complete character state as a normal existing-character login.
        var hydratedCharacter = await charRepo.GetByIdAsync(newChar.Id, ct);
        if (hydratedCharacter is null)
        {
            _logger.LogError(
                "Character {CharacterId} could not be reloaded after creation for user {UserId}",
                newChar.Id,
                session.User.Id
            );
            return new AvatarCreateResponse(1);
        }

        var staleCharacter = session.User.Characters.FirstOrDefault(character =>
            character.Id == hydratedCharacter.Id
        );
        if (staleCharacter is not null)
            session.User.Characters.Remove(staleCharacter);
        session.User.Characters.Add(hydratedCharacter);

        // The new-character client flow does not issue another AvatarGetDataRequest.
        // Populate its selected slot before Enquete completion leads directly to select_avatar.
        var avatarData = AvatarGetDataHandler.CreateDataResponse(hydratedCharacter, request.slotId);
        await session.SendAsync(PacketType.AvatarDataResponse, avatarData.ToBytes(), ct);

        return new AvatarCreateResponse(0);
    }
}
