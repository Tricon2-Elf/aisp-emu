using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public sealed class UccAdvFigureBaseListResponse(uint result, IReadOnlyList<UccAdvFigure> figures)
    : IOutgoingPacket
{
    public const int MaximumEntryCount = 100;

    public UccAdvFigureBaseListResponse()
        : this(0, Array.Empty<UccAdvFigure>()) { }

    public uint Result { get; } = result;
    public IReadOnlyList<UccAdvFigure> Figures { get; } = figures;

    public byte[] ToBytes()
    {
        if (Figures.Count > MaximumEntryCount)
            throw new InvalidOperationException(
                $"{nameof(UccAdvFigureBaseListResponse)} supports at most {MaximumEntryCount} entries, received {Figures.Count}."
            );

        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write((uint)Figures.Count);
        foreach (var figure in Figures)
            figure.Write(writer);
        return writer.ToBytes();
    }
}
