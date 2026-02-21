using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers;

public class AreaAvatarGetDataHandler(ILogger<AreaAvatarGetDataHandler> logger, ICharacterRepository charRepo) : IPacketHandler
{
    public PacketType RequestType => PacketType.AvatarGetDataRequest;

    public PacketType ResponseType => PacketType.AvatarNotifyData;

    public MessageDomain Domain => MessageDomain.Area;

    private readonly ILogger<AreaAvatarGetDataHandler> _logger = logger;
    private readonly ICharacterRepository _charRepo = charRepo;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        //Check if the client is authenticated and has a character
        if (!connection.IsAuthenticated || connection.User == null || connection.User.Characters.First() == null)
            return;
        _logger.LogInformation("Received AvatarGetDataRequest from Client: {Id}, IsAuthed: {auth}", connection.Id, connection.IsAuthenticated);
        _logger.LogInformation("Received AvatarGetDataRequest from Client: {Id}", connection.Id);

        var cha = connection.User!.Characters.First();

        _logger.LogInformation("Processing AvatarGetDataRequest for Character: {CharacterName} (ID: {CharacterId})", cha.Name, cha.Id);
        var charaData = CreateCData(cha, new MovementData(0f, 0f, 0f, 0, MovementType.Stopped), 0);
        var avatarData = new AvatarData((uint)cha.Id, charaData);
        var notifyData = new AvatarNotifyData(0, avatarData);
        await connection.SendAsync(ResponseType, notifyData.ToBytes(), ct);
    }

    private static CharaData CreateCData(DAL.Entities.Character cha, MovementData pos, uint slotId)
    {
        var cd = new CharaData(slotId, (uint)cha.ModelId, cha.Name) { moveData = pos };
        cd.Visual.VisualId = (uint)cha.Id;
        cd.Visual.BloodType = cha.BloodType;
        cd.Visual.Month = (byte)cha.Birthdate.Month;
        cd.Visual.Day = (byte)cha.Birthdate.Day;
        cd.Visual.Gender = (uint)cha.Gender;
        cd.Visual.Face = (byte)cha.FaceType;
        cd.Visual.Hairstyle = cha.Hairstyle;
        for (byte s = 0; s < 30; s++)
        {
            var eq = cha.Equipment.FirstOrDefault(e => e.SlotIndex == s);
            cd.AddEquip(eq != null ? (uint)eq.ItemId : 0, s);
        }
        return cd;
    }
}
