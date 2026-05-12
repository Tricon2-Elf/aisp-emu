using AISpace.Common.DAL.Entities;
using AISpace.Network.Data;

namespace AISpace.Common.Game;

internal static class ItemEntityMapper
{
    public static ItemData ToItemBaseListData(Item item)
    {
        var id = (uint)item.Id;
        var socket = (uint)item.Socket;
        var iconId = (uint)item.IconId;

        uint category = 1;
        if (socket == 2)
            category = 2;
        if (socket == 4)
            category = 8;
        if (socket == 8)
            category = 8;
        if (socket == 16)
            category = 4;

        return new ItemData
        {
            Key = id,
            SortedListPriority = id,
            ItemId = id,
            IconId = iconId,
            Name = item.Name,
            Socket1 = socket,
            Socket2 = socket,
            Category = category,
        };
    }
}
