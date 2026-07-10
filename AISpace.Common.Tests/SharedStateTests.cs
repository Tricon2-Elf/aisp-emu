using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using AISpace.Network;

namespace AISpace.Common.Tests;

public class SharedStateTests
{
    [Fact]
    public void GetOrAddSession_ReturnsSameInstance_ForSameConnectionId()
    {
        var state = new SharedState();
        var id = Guid.NewGuid();
        var a = state.GetOrAddSession(id, () => new FakeSession(id));
        var b = state.GetOrAddSession(id, () => new FakeSession(id));
        Assert.Same(a, b);
    }

    [Fact]
    public void RegisterClient_Area_RemovesGhostWithSameCharacterId()
    {
        var state = new SharedState();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var s1 = new FakeSession(id1) { CharacterId = 99 };
        var s2 = new FakeSession(id2) { CharacterId = 99 };
        state.RegisterClient(ServerType.Area, s1);
        Assert.Contains(state.AreaClients, client => client.ConnectionId == id1);
        state.RegisterClient(ServerType.Area, s2);
        Assert.DoesNotContain(state.AreaClients, client => client.ConnectionId == id1);
        Assert.Contains(state.AreaClients, client => client.ConnectionId == id2);
    }

    [Fact]
    public void UnregisterClient_RemovesFromAllMaps()
    {
        var state = new SharedState();
        var id = Guid.NewGuid();
        var s = new FakeSession(id);
        state.RegisterClient(ServerType.Auth, s);
        state.RegisterClient(ServerType.Msg, s);
        state.RegisterClient(ServerType.Area, s);
        state.GetOrAddSession(id, () => s);
        state.UnregisterClient(ServerType.Auth, id);
        Assert.DoesNotContain(state.AuthClients, client => client.ConnectionId == id);
        Assert.DoesNotContain(state.MsgClients, client => client.ConnectionId == id);
        Assert.DoesNotContain(state.AreaClients, client => client.ConnectionId == id);
    }

    [Fact]
    public async Task BroadcastAreaDisappearAsync_SendsNotifyDisappearToPeersInSameArea()
    {
        var state = new SharedState();
        var source = new FakeSession(Guid.NewGuid())
        {
            CharacterId = 1001,
            MapId = 10990100,
            ChannelId = 1,
        };
        var sameAreaPeer = new FakeSession(Guid.NewGuid())
        {
            CharacterId = 1002,
            MapId = 10990100,
            ChannelId = 1,
        };
        var otherMapPeer = new FakeSession(Guid.NewGuid())
        {
            CharacterId = 1003,
            MapId = 10990200,
            ChannelId = 1,
        };

        state.RegisterClient(ServerType.Area, source);
        state.RegisterClient(ServerType.Area, sameAreaPeer);
        state.RegisterClient(ServerType.Area, otherMapPeer);

        await state.BroadcastAreaDisappearAsync(source, TestContext.Current.CancellationToken);

        Assert.Contains(sameAreaPeer.Sent, p => p.Type == PacketType.NotifyDisappearChara);
        Assert.DoesNotContain(otherMapPeer.Sent, p => p.Type == PacketType.NotifyDisappearChara);
    }

    private sealed class FakeSession(Guid connectionId) : IPlayerSession
    {
        public Guid ConnectionId { get; } = connectionId;
        public int UserId { get; set; }
        public uint CharacterId { get; set; }
        public Character? Character { get; set; }
        public User? User { get; set; }
        public uint MapId { get; set; }
        public int ChannelId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public sbyte Rotation { get; set; }
        public int MovementTypeId { get; set; }
        public bool HasMovedSinceMapLoad { get; set; }
        public bool IsMapTransitionPending { get; set; }
        public bool NeedsPostLoadSelfAvatarNotify { get; set; }
        public PendingAreaMapSelection? PendingAreaMapSelection { get; set; }
        public int? ActiveShopId { get; set; }
        public bool PendingEventEndAfterFade { get; set; }
        public bool IsAuthenticated => User != null;
        public List<(PacketType Type, byte[] Payload)> Sent { get; } = [];

        public Task SendAsync(PacketType type, byte[] payload, CancellationToken ct = default)
        {
            Sent.Add((type, payload));
            return Task.CompletedTask;
        }
    }
}
