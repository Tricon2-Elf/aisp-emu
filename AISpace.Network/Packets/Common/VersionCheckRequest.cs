namespace AISpace.Network.Packets.Common
{
    using AISpace.Network;

    public class VersionCheckRequest(uint Major, uint Minor, uint Version) : IIncomingPacket<VersionCheckRequest>
    {
        public uint Major = Major;
        public uint Minor = Minor;
        public uint Version = Version;

        public static VersionCheckRequest FromBytes(ReadOnlySpan<byte> data)
        {
            PacketReader reader = new(data);
            uint major = reader.ReadUInt();
            uint minor = reader.ReadUInt();
            uint version = reader.ReadUInt();
            return new VersionCheckRequest(major, minor, version);
        }
    }
}
