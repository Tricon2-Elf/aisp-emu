using AISpace.Network.Data;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Tests;

public class ItemTryEquipReplaceTests
{
    [Fact]
    public void FromBytes_parses_obj_id_and_equip_list()
    {
        var payload = Convert.FromHexString(
            "01000000" + "02000000" + "F444A30000080000" + "FC1D9A0008000000"
        );
        var request = ItemTryEquipReplaceRequest.FromBytes(payload);

        Assert.Equal(1u, request.ObjId);
        Assert.Equal(2, request.Equips.Count);
        Assert.Equal(0x00A344F4u, request.Equips[0].ItemId);
        Assert.Equal(2048u, request.Equips[0].SocketBit);
        Assert.Equal(0x009A1DFCu, request.Equips[1].ItemId);
        Assert.Equal(8u, request.Equips[1].SocketBit);
    }

    [Fact]
    public void ReplacedNotify_round_trips_equip_entries()
    {
        var equips = new[] { new ItemEquipEntry(10100220, 8), new ItemEquipEntry(10700020, 2048) };
        var payload = new ItemTryEquipReplacedNotify(1, equips).ToBytes();

        Assert.Equal(1u, BitConverter.ToUInt32(payload, 0));
        Assert.Equal(2u, BitConverter.ToUInt32(payload, 4));
        Assert.Equal(16 + 8, payload.Length);
    }
}
