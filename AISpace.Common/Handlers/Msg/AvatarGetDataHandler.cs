using AISpace.Common.Network.Packets.Msg;
using AISpace.Network;

namespace AISpace.Common.Handlers.Msg;

public class AvatarGetDataHandler(ILogger<AvatarGetDataHandler> logger, ICharacterRepository charRepo) : IPacketHandler
{
    public PacketType RequestType => PacketType.AvatarGetDataRequest;

    public PacketType ResponseType => PacketType.AvatarDataResponse;

    public MessageDomain Domain => MessageDomain.Msg;

    ILogger<AvatarGetDataHandler> _logger = logger;
    ICharacterRepository _charRepo = charRepo;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        if (!connection.IsAuthenticated)
            return;

        _logger.LogWarning("Client: {ClientId} requested AvatarGetData ", connection.Id);

        if (connection.User!.Characters.Count != 0)
        {
            Character cha = connection.User!.Characters.First();

            var dataResponse = new AvatarDataResponse((uint)cha.Id, cha.Name, cha.ModelId, 0, 0);
            dataResponse.Visual.VisualId = (uint)cha.Id;
            dataResponse.Visual.BloodType = cha.BloodType;
            dataResponse.Visual.Month = (byte)cha.Birthdate.Month;
            dataResponse.Visual.Day = (byte)cha.Birthdate.Day;
            dataResponse.Visual.Gender = (uint)cha.Gender;
            dataResponse.Visual.Face = (byte)cha.FaceType;
            dataResponse.Visual.Hairstyle = cha.Hairstyle;
            // Fill 30 slots by SlotIndex so client gets correct slot mapping
            for (byte slot = 0; slot < 30; slot++)
            {
                var eq = cha.Equipment.FirstOrDefault(e => e.SlotIndex == slot);
                dataResponse.AddEquip(eq != null ? (uint)eq.ItemId : 0, slot);
            }
            await connection.SendAsync(ResponseType, dataResponse.ToBytes(), ct);
        }
        var avatarGetDataResp = new AvatarGetDataResponse(0);
        await connection.SendAsync(PacketType.AvatarGetDataResponse, avatarGetDataResp.ToBytes(), ct);
    }
}
