using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaMapEnterHandler(IMapRepository mapRepository, ICharacterRepository characterRepository, SharedState state, ILogger<AreaMapEnterHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.MapEnterRequest;
    public PacketType ResponseType => PacketType.MapEnterResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = AreaMapEnterRequest.FromBytes(payload.Span);
        logger.LogInformation("MapEnterRequest from user {UserId}: requested MapID {MapId}, ChannelID {ChannelId}", session.User?.Id ?? session.UserId, request.MapID, request.ChannelId);

        var map = await mapRepository.GetByMapIdAsync(request.MapID, ct);
        if (map == null)
        {
            logger.LogWarning("Rejecting MapEnterRequest for unknown MapID {MapId}", request.MapID);
            await session.SendAsync(ResponseType, new AreaMapEnterResponse(1).ToBytes(), ct);
            return;
        }

        var character = await ResolveCharacterAsync(session, characterRepository, ct);
        if (character == null)
        {
            logger.LogWarning("Rejecting MapEnterRequest for user {UserId}: character could not be resolved", session.User?.Id ?? session.UserId);
            await session.SendAsync(ResponseType, new AreaMapEnterResponse(1).ToBytes(), ct);
            return;
        }

        var oldPeers = state.GetAreaPeers(session).ToList();
        var disappearPacket = new NotifyDisappearChara(session.CharacterId).ToBytes();
        foreach (var other in oldPeers)
        {
            await other.SendAsync(PacketType.NotifyDisappearChara, disappearPacket, ct);
        }

        var updatedCharacter = await characterRepository.UpdateCurrentMapAsync(character.Id, request.MapID, ct) ?? character;
        updatedCharacter.CurrentMapId = request.MapID;

        session.Character = updatedCharacter;
        session.CharacterId = (uint)updatedCharacter.Id;
        session.MapId = request.MapID;
        session.ChannelId = (int)request.ChannelId;
        session.X = map.SpawnX;
        session.Y = map.SpawnY;
        session.Z = map.SpawnZ;
        session.Rotation = (sbyte)map.SpawnRotation;
        session.MovementTypeId = (int)MovementType.Stopped;

        var userCharacter = session.User?.Characters.FirstOrDefault(candidate => candidate.Id == updatedCharacter.Id);
        if (userCharacter != null)
            userCharacter.CurrentMapId = request.MapID;

        var response = new AreaMapEnterResponse(0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }

    private static async Task<DAL.Entities.Character?> ResolveCharacterAsync(IPlayerSession session, ICharacterRepository characterRepository, CancellationToken ct)
    {
        if (session.Character != null)
            return session.Character;

        if (session.CharacterId != 0)
            return await characterRepository.GetByIdAsync((int)session.CharacterId, ct);

        var fallback = session.User?.Characters.FirstOrDefault();
        if (fallback == null)
            return null;

        return await characterRepository.GetByIdAsync(fallback.Id, ct) ?? fallback;
    }
}
