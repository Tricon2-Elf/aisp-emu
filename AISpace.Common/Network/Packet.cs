namespace AISpace.Common.Network;

public record Packet(ClientContext Client, PacketType Type, byte[] Data, ushort RawType);
