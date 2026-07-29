using AISpace.Common.DAL.Entities;
using AISpace.Network.Data;

namespace AISpace.Common.Game;

internal static class TryEquipNotifyBuilder
{
    /// <summary>
    /// Builds the equip list the client expects in recv_item_try_equip_replaced.
    /// Used to snap the preview avatar back to persisted (pre-wardrobe) equipment on cancel.
    /// </summary>
    public static List<ItemEquipEntry> FromCharacter(Character character)
    {
        var entries = new List<ItemEquipEntry>();
        foreach (var equip in character.Equipment.OrderBy(e => e.SlotIndex))
        {
            if (equip.ItemId == 0)
                continue;

            var socket = ItemEntityMapper.ResolveBodyspot(equip.ItemId, name: equip.Item?.Name);
            entries.Add(new ItemEquipEntry((uint)equip.ItemId, socket));
        }

        return entries;
    }

    /// <summary>
    /// Builds the complete fixed-size equipment array consumed by recv_notify_update_robo_equip.
    /// Robo equipment is positional, so empty slots must be retained.
    /// </summary>
    public static List<ItemEquipEntry> FromRobo(RoboData robo)
    {
        return robo
            .Character.Equips.Select(equip => new ItemEquipEntry(equip.ItemId, equip.Socket))
            .ToList();
    }
}
