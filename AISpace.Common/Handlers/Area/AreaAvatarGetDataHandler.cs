using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaAvatarGetDataHandler(ILogger<AreaAvatarGetDataHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AvatarGetDataRequest;
    public PacketType ResponseType => PacketType.AvatarNotifyData;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        session.NeedsPostLoadSelfAvatarNotify = false;

        var cha = session.User!.Characters.First();
        var pos = new MovementData(session.X, session.Y, session.Z, session.Rotation, (MovementType)session.MovementTypeId);

        var cd = new CharaData((uint)cha.Id, cha.ModelId, cha.Name) { MoveData = pos };
        cd.Visual.VisualId = (uint)cha.Id;
        cd.Visual.BloodType = cha.BloodType;
        cd.Visual.Month = (byte)cha.Birthdate.Month;
        cd.Visual.Day = (byte)cha.Birthdate.Day;
        cd.Visual.Gender = (uint)cha.Gender;
        cd.Visual.Face = (byte)cha.FaceType;
        cd.Visual.Hairstyle = cha.Hairstyle;

        cd.AddEquip(cha.Equipment.Select(e => new CharacterEquipSlot(e.SlotIndex, (uint)e.ItemId)), ItemEntityMapper.ResolveEquipSocket);

        var avatarData = new AvatarData((uint)cha.Id, cd);
        logger.LogInformation("Sending AvatarNotifyData to {ConnectionId} for character {CharacterId}", session.ConnectionId, cha.Id);
        await session.SendAsync(ResponseType, new AvatarNotifyData(0, avatarData).ToBytes(), ct);
    }
}
