using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public static class CircleNotifyHelper
{
    public static async Task SendRosterAsync(
        ICircleRepository circles,
        SharedState state,
        int circleId,
        CancellationToken ct
    )
    {
        var circle = await circles.GetByIdAsync(circleId, ct);
        if (circle is null)
            return;

        var members = await circles.GetMembersAsync(circleId, ct);
        var online = state
            .GetOnlineMsgClientsByCharacterIds(members.Select(m => m.CharacterId))
            .Select(s => (int)s.CharacterId)
            .ToHashSet();

        CircleMemberData[] memberData =
        [
            .. members.Select(m => new CircleMemberData
            {
                AvatarId = (uint)m.CharacterId,
                Name = m.Character?.Name ?? string.Empty,
                Role = m.AuthLevel,
            }),
        ];
        bool[] loginFlags = [.. members.Select(m => online.Contains(m.CharacterId))];
        var payload = new CircleNotifyMember((ulong)circleId, memberData, loginFlags).ToBytes();

        foreach (
            var client in state.GetOnlineMsgClientsByCharacterIds(
                members.Select(m => m.CharacterId)
            )
        )
            await client.SendAsync(PacketType.CircleNotifyMember, payload, ct);
    }

    public static async Task NotifyMembersAsync(
        ICircleRepository circles,
        SharedState state,
        int circleId,
        PacketType type,
        byte[] payload,
        CancellationToken ct,
        int? excludeCharacterId = null
    )
    {
        var members = await circles.GetMembersAsync(circleId, ct);
        var ids = members
            .Select(m => m.CharacterId)
            .Where(id => excludeCharacterId is null || id != excludeCharacterId.Value);
        foreach (var client in state.GetOnlineMsgClientsByCharacterIds(ids))
            await client.SendAsync(type, payload, ct);
    }

    public static async Task NotifyMemberLoginAsync(
        ICircleRepository circles,
        SharedState state,
        int characterId,
        CancellationToken ct
    )
    {
        var memberships = await circles.GetMembershipsForCharacterAsync(characterId, ct);
        foreach (var (circle, _) in memberships)
        {
            var payload = new CircleNotifyMemberLogin(
                (ulong)circle.Id,
                (uint)characterId
            ).ToBytes();
            await NotifyMembersAsync(
                circles,
                state,
                circle.Id,
                PacketType.CircleNotifyMemberLogin,
                payload,
                ct,
                excludeCharacterId: characterId
            );
        }
    }

    public static async Task NotifyMemberLogoutAsync(
        ICircleRepository circles,
        SharedState state,
        int characterId,
        CancellationToken ct
    )
    {
        var memberships = await circles.GetMembershipsForCharacterAsync(characterId, ct);
        foreach (var (circle, _) in memberships)
        {
            var payload = new CircleNotifyMemberLogout(
                (ulong)circle.Id,
                (uint)characterId
            ).ToBytes();
            await NotifyMembersAsync(
                circles,
                state,
                circle.Id,
                PacketType.CircleNotifyMemberLogout,
                payload,
                ct,
                excludeCharacterId: characterId
            );
        }
    }
}
