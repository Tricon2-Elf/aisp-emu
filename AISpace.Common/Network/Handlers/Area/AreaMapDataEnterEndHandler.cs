using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using AISpace.Common.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers;

public class AreaMapDataEnterEndHandler(ILogger<AreaMapDataEnterEndHandler> _logger, SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.MapDataEnterEndRequest;

    public PacketType ResponseType => PacketType.MapDataEnterEndResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        await connection.SendAsync(ResponseType, new MapDataEnterEndResponse().ToBytes(), ct);
        if (connection.User == null)
        {
            _logger.LogWarning("User not found for connection: {ConnectionId}", connection.Id);
            return;
        }
        var myChar = connection.User!.Characters.First();
        var myPos = new MovementData(0f, 0f, 0f, 0, MovementType.Stopped);
        var spawnMeForOthers = new AvatarNotifyData(0, new AvatarData(1, CreateCData(myChar, myPos))).ToBytes();

        foreach (var other in state.AreaClients.Values)
        {
            if (other.Id == connection.Id)
                continue;

            await other.SendAsync(PacketType.AvatarNotifyData, spawnMeForOthers, ct);
            await Task.Delay(100, ct);

            var otherChar = other.User?.Characters.FirstOrDefault();
            if (otherChar != null)
            {
                var otherPos = new MovementData(0f, 0f, 0f, 0, MovementType.Stopped);
                var spawnOtherForMe = new AvatarNotifyData(0, new AvatarData(1, CreateCData(otherChar, otherPos))).ToBytes();
                await connection.SendAsync(PacketType.AvatarNotifyData, spawnOtherForMe, ct);
                await Task.Delay(100, ct);
            }
        }

        for (int i = 0; i < 100; i++)
        {
            await SpawnFakePlayer(connection, ct);
        }
    }

    private static async Task SpawnFakePlayer(ClientConnection connection, CancellationToken ct)
    {
        var cha = connection.User!.Characters.First();
        var rndx = Random.Shared.Next(100, 3000);
        var rndy = Random.Shared.Next(100, 3000);
        var Pos = new MovementData(432f - rndx, -0f, 888f - rndy, (sbyte)Random.Shared.Next(-128, 128), MovementType.Stopped);
        cha.Id = Random.Shared.Next(1000000, 9999999);
        var hairStyles = new[] { 10920010, 10920020, 10920040 };
        cha.Name = $"FakePlayer{cha.Id}";
        cha.FaceType = (uint)Random.Shared.Next(1, 4);
        cha.Hairstyle = (uint)hairStyles[Random.Shared.Next(0, 3)];
        cha.Equipment.Clear();

        var shirtIds = new[] { 10100210, 10100211, 10100212, 10100220, 10100221, 10100222, 10100223, 10100224, 10100225, 10100226 };
        cha.Equipment.Add(new CharacterEquipment { SlotIndex = 0, ItemId = shirtIds[Random.Shared.Next(shirtIds.Length)] }); //Shirt
        cha.Equipment.Add(new CharacterEquipment { SlotIndex = 1, ItemId = 10200100 }); //Pants
        cha.Equipment.Add(new CharacterEquipment { SlotIndex = 2, ItemId = 10400030 }); //Socks
        cha.Equipment.Add(new CharacterEquipment { SlotIndex = 3, ItemId = 10500070 }); //Shoes

        var spawnFake = new AvatarNotifyData(0, new AvatarData(2, CreateCData(cha, Pos))).ToBytes();
        await connection.SendAsync(PacketType.AvatarNotifyData, spawnFake, ct);
    }

    private static CharaData CreateCData(Character cha, MovementData pos)
    {
        var cd = new CharaData((uint)cha.Id, cha.ModelId, cha.Name) { moveData = pos };
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
