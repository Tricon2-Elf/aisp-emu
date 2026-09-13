using aisp.Network;

namespace aisp.Network.Data;

/// <summary>
/// One drama figure in <c>recv_get_ucc_adv_figure_base_list_r</c>: a doll the drama notebook's
/// figure picker offers, grouped by its box. 377 bytes on the wire (the client's parser at
/// <c>0x79B1E0</c>; in memory the record is 0x17C apart).
/// </summary>
public sealed class UccAdvFigure
{
    public const int WireSize = 377;
    public const int NameBytes = 96;
    public const int ExtraFieldCount = 7;
    public const int EquipSlotCount = 30;

    /// <summary>The picker looks figures up by a 16-bit id, so this must fit in a word.</summary>
    public uint FigureId { get; init; }

    /// <summary>Inventory artwork used by the shop: item/icon/{IconId:D8}.dds.</summary>
    public uint IconId { get; init; }

    /// <summary>The box the figure sits in: the folder of its logo and package art.</summary>
    public uint BoxId { get; init; }
    public string Name { get; init; } = "";
    public bool Owned { get; init; } = true;

    /// <summary>Character gender used by clothing validation: 1 male, 2 female.</summary>
    public uint Gender { get; init; }

    /// <summary>How many people the figure is: 1, 2 or 3.</summary>
    public uint People { get; init; } = 1;

    /// <summary>Index of the box art, figure/{box}/package_{n}.dds.</summary>
    public uint PackageId { get; init; }

    /// <summary>Face variant passed to doll creation, independent of package artwork.</summary>
    public uint Face { get; init; }

    /// <summary>Base hair item ID passed to doll creation, separate from equipped wigs.</summary>
    public uint Hairstyle { get; init; }

    /// <summary>The seven-digit drama model the 3D doll is built from.</summary>
    public uint ModelId { get; init; }

    /// <summary>Item ids of the clothes and wig the doll wears; an empty list is a nude doll.</summary>
    public IReadOnlyList<uint> Equipment { get; init; } = [];

    public void Write(PacketWriter writer)
    {
        writer.Write(FigureId);
        writer.Write(IconId);
        writer.WriteFixedStringNulTerminated(Name, NameBytes);
        writer.Write(Owned ? (byte)1 : (byte)0);
        // The ingest at 0x47a900 copies the rest into the client's figure record: the low
        // byte of +0x6C is gender; +0x70 the people count (1, 2 or 3, anything else counts as none);
        // +0x74 and +0x78 are face and hairstyle (0x6c6c87 / 0x6c6c99); +0x7C is the model;
        // +0x80 and +0x84 are the %d's of ./interface/figure/%d/package_%d.dds (string table
        // 10/10/11) and +0x80 the folder of logo.dds as well; then thirty equipment item ids
        // the doll is dressed with (0x4066c0 hands each to 0x41d810 together with the word
        // at the same index in the second block of thirty). The second block and the tail
        // word are kept 0.
        writer.Write(Gender);
        writer.Write(People);
        writer.Write(Face);
        writer.Write(Hairstyle);
        writer.Write(ModelId);
        writer.Write(BoxId);
        writer.Write(PackageId);
        for (var i = 0; i < EquipSlotCount; i++)
            writer.Write(i < Equipment.Count ? Equipment[i] : 0u);
        for (var i = 0; i < EquipSlotCount + 1; i++)
            writer.Write(0u);
    }
}
