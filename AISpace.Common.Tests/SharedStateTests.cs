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
        state.RegisterClient("Area", s1);
        Assert.True(state.AreaClients.ContainsKey(id1));
        state.RegisterClient("Area", s2);
        Assert.False(state.AreaClients.ContainsKey(id1));
        Assert.True(state.AreaClients.ContainsKey(id2));
    }

    [Fact]
    public void UnregisterClient_RemovesFromAllMaps()
    {
        var state = new SharedState();
        var id = Guid.NewGuid();
        var s = new FakeSession(id);
        state.AuthClients[id] = s;
        state.MsgClients[id] = s;
        state.AreaClients[id] = s;
        state.GetOrAddSession(id, () => s);
        state.UnregisterClient("Auth", id);
        Assert.False(state.AuthClients.ContainsKey(id));
        Assert.False(state.MsgClients.ContainsKey(id));
        Assert.False(state.AreaClients.ContainsKey(id));
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
        public bool IsAuthenticated => User != null;

        public Task SendAsync(PacketType type, byte[] payload, CancellationToken ct = default) => Task.CompletedTask;
    }
}
