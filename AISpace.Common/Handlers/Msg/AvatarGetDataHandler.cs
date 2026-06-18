using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Msg;

public class AvatarGetDataHandler(ILogger<AvatarGetDataHandler> logger, ICharacterRepository charRepo) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AvatarGetDataRequest;

    public PacketType ResponseType => PacketType.AvatarDataResponse;

    public ServerType ServerType => ServerType.Msg;

    ILogger<AvatarGetDataHandler> _logger = logger;
    ICharacterRepository _charRepo = charRepo;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        if (session.User!.Characters.Count != 0)
        {
            Character cha = session.User!.Characters.First();

            var dataResponse = new AvatarDataResponse((uint)cha.Id, cha.Name, cha.ModelId, 0, 0);
            dataResponse.Visual.VisualId = (uint)cha.Id;
            dataResponse.Visual.BloodType = cha.BloodType;
            dataResponse.Visual.Month = (byte)cha.Birthdate.Month;
            dataResponse.Visual.Day = (byte)cha.Birthdate.Day;
            dataResponse.Visual.Gender = (uint)cha.Gender;
            dataResponse.Visual.Face = (byte)cha.FaceType;
            dataResponse.Visual.Hairstyle = cha.Hairstyle;
            dataResponse.AddEquip(
                cha.Equipment.Select(e => new CharacterEquipSlot(e.SlotIndex, (uint)e.ItemId)),
                ItemEntityMapper.ResolveBodyspot
            );
            await session.SendAsync(ResponseType, dataResponse.ToBytes(), ct);
        }
        var avatarGetDataResp = new AvatarGetDataResponse(0);
        await session.SendAsync(PacketType.AvatarGetDataResponse, avatarGetDataResp.ToBytes(), ct);
    }
}
