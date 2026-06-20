using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaNpcGetDataHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.NpcGetDataRequest;

    public PacketType ResponseType => PacketType.NpcGetDataResponse;

    public ServerType ServerType => ServerType.Area;

    /// <summary>First NPC object ID to use when spawning NPCs (above typical character IDs to avoid clashes).</summary>
    private const uint NpcObjectIdBase = 0x50000001;

    /// <summary>Example NPC model ID (game-specific; adjust per asset).</summary>
    private const uint DefaultNpcModelId = 1001021;

    /// <summary>When true, send NpcNotifyData after NpcGetDataResponse. Packet layout matches client ReadNpcData (result, CharaData, 1 byte).</summary>
    private const bool SendNpcNotifyData = true;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new NpcGetDataResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);

        if (SendNpcNotifyData)
        {
            // Spawn one or more NPCs for this player: same position as avatar spawn flow.
            // NPC appears a few units in front of the player.
            float nx = session.X + 200f;
            float nz = session.Z + 200f;
            var pos = new MovementData(nx, session.Y, nz, session.Rotation, MovementType.Stopped);
            var sourceChar = session.User?.Characters.FirstOrDefault();
            uint modelId = sourceChar?.ModelId ?? DefaultNpcModelId;

            var npcChara = new CharaData(NpcObjectIdBase, modelId, "NPC") { MoveData = pos };
            if (sourceChar != null)
            {
                npcChara.Visual.BloodType = sourceChar.BloodType;
                npcChara.Visual.Month = (byte)sourceChar.Birthdate.Month;
                npcChara.Visual.Day = (byte)sourceChar.Birthdate.Day;
                npcChara.Visual.Gender = (uint)sourceChar.Gender;
                npcChara.Visual.VisualId = NpcObjectIdBase;
                npcChara.Visual.Face = (byte)sourceChar.FaceType;
                npcChara.Visual.Hairstyle = sourceChar.Hairstyle;
            }

            var npcPacket = new NpcNotifyData(0, NpcObjectIdBase, npcChara).ToBytes();
            //await session.SendAsync(PacketType.NpcNotifyData, npcPacket, ct);
        }
    }
}
