using AISpace.Common;
using AISpace.Common.DAL;
using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Portal;
using AISpace.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Server;

internal static class PortalApiEndpointsExtensions
{
    internal static WebApplication MapPortalBackendApiEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth/portal")
            .WithTags("Portal Auth API")
            .AddEndpointFilter<PortalApiEndpointFilter>();
        var msg = app.MapGroup("/api/msg/portal")
            .WithTags("Portal Msg API")
            .AddEndpointFilter<PortalApiEndpointFilter>();
        var area = app.MapGroup("/api/area/portal")
            .WithTags("Portal Area API")
            .AddEndpointFilter<PortalApiEndpointFilter>();

        auth.MapPost("/register", RegisterAsync);
        auth.MapPost("/session", LoginAsync);
        auth.MapGet("/users", ListUsersAsync);
        auth.MapGet("/users/{userId:int}", GetUserAsync);
        auth.MapPost("/users/{userId:int}/ban", BanAsync);
        auth.MapPost("/users/{userId:int}/unban", UnbanAsync);
        auth.MapPost("/users/{userId:int}/password", SetPasswordAsync);
        auth.MapPost("/users/{userId:int}/password/change", ChangePasswordAsync);
        auth.MapPost(
            "/users/{userId:int}/disconnect",
            (int userId, ServerTypeSessionService sessions, CancellationToken ct) =>
                DisconnectAsync(userId, ServerType.Auth, sessions, ct)
        );

        msg.MapPost(
            "/users/{userId:int}/disconnect",
            (int userId, ServerTypeSessionService sessions, CancellationToken ct) =>
                DisconnectAsync(userId, ServerType.Msg, sessions, ct)
        );
        area.MapGet("/users/{userId:int}/account", GetAccountAsync);
        area.MapPost("/users/summaries", GetSummariesAsync);
        area.MapPost(
            "/users/{userId:int}/disconnect",
            (int userId, ServerTypeSessionService sessions, CancellationToken ct) =>
                DisconnectAsync(userId, ServerType.Area, sessions, ct)
        );
        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterPortalAccountRequest request,
        IUserRepository users,
        CancellationToken ct
    )
    {
        var username = request.Username.Trim();
        if (
            !IsValidUsername(username)
            || string.IsNullOrWhiteSpace(request.Password)
            || request.Password != request.ConfirmPassword
            || request.Password.Length is < 8 or > 128
        )
            return TypedResults.BadRequest(new PortalErrorDto("Invalid registration details."));
        if (await users.GetByUsernameAsync(username) is not null)
            return TypedResults.Conflict(new PortalErrorDto("Username already exists."));

        await users.AddAsync(username, request.Password);
        var user = await users.GetByUsernameAsync(username);
        if (user is null)
            return TypedResults.Problem(
                "Account creation failed.",
                statusCode: StatusCodes.Status500InternalServerError
            );

        await users.TouchLastLoggedInAsync(user.Id, ct);
        return TypedResults.Ok(new PortalIdentityDto(user.Id, user.Username));
    }

    private static async Task<IResult> LoginAsync(
        PortalLoginRequest request,
        IUserRepository users,
        ILoggerFactory loggerFactory,
        CancellationToken ct
    )
    {
        var user = await users.AuthenticateAsync(request.Username, request.Password);
        if (user is null)
        {
            loggerFactory
                .CreateLogger("PortalAuthApi")
                .LogInformation(
                    "Portal login rejected for {Username}: invalid credentials",
                    request.Username
                );
            return TypedResults.Unauthorized();
        }
        if (user.IsBanned)
        {
            loggerFactory
                .CreateLogger("PortalAuthApi")
                .LogInformation(
                    "Portal login rejected for {Username}: account is banned",
                    user.Username
                );
            return TypedResults.Unauthorized();
        }

        await users.TouchLastLoggedInAsync(user.Id, ct);
        return TypedResults.Ok(new PortalIdentityDto(user.Id, user.Username));
    }

    private static async Task<IResult> ListUsersAsync(
        string? search,
        int? skip,
        int? take,
        IUserRepository users,
        CancellationToken ct
    )
    {
        var actualSkip = Math.Max(skip ?? 0, 0);
        var actualTake = Math.Clamp(take ?? 50, 1, 100);
        var result = await users.GetAllAsync(search, actualSkip, actualTake);
        var total = await users.CountAsync(search);
        return TypedResults.Ok(new PortalUserPageDto(result.Select(MapSummary).ToArray(), total));
    }

    private static async Task<IResult> GetUserAsync(
        int userId,
        IUserRepository users,
        CancellationToken ct
    )
    {
        var user = await users.GetById(userId);
        return user is null
            ? TypedResults.NotFound(new PortalErrorDto("User not found."))
            : TypedResults.Ok(MapDetail(user));
    }

    private static async Task<IResult> BanAsync(
        int userId,
        PortalBanRequest request,
        IUserRepository users,
        CancellationToken ct
    )
    {
        var user = await users.GetById(userId);
        if (user is null)
            return TypedResults.NotFound(new PortalErrorDto("User not found."));
        await users.SetBannedAsync(userId, true, request.Reason);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> UnbanAsync(
        int userId,
        IUserRepository users,
        CancellationToken ct
    )
    {
        if (await users.GetById(userId) is null)
            return TypedResults.NotFound(new PortalErrorDto("User not found."));
        await users.SetBannedAsync(userId, false);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> SetPasswordAsync(
        int userId,
        PortalSetPasswordRequest request,
        IUserRepository users,
        CancellationToken ct
    )
    {
        if (!IsValidPassword(request.NewPassword, request.ConfirmPassword))
            return TypedResults.BadRequest(
                new PortalErrorDto(
                    "The new password must be 8 to 128 characters and both entries must match."
                )
            );
        if (await users.GetById(userId) is null)
            return TypedResults.NotFound(new PortalErrorDto("User not found."));

        await users.UpdatePasswordAsync(userId, request.NewPassword);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ChangePasswordAsync(
        int userId,
        PortalChangePasswordRequest request,
        IUserRepository users,
        CancellationToken ct
    )
    {
        if (
            !IsValidPassword(request.NewPassword, request.ConfirmPassword)
            || string.IsNullOrWhiteSpace(request.CurrentPassword)
        )
            return TypedResults.BadRequest(
                new PortalErrorDto(
                    "The new password must be 8 to 128 characters and both entries must match."
                )
            );

        var user = await users.GetById(userId);
        if (user is null)
            return TypedResults.NotFound(new PortalErrorDto("User not found."));
        if (!user.VerifyPassword(request.CurrentPassword))
            return Results.Json(
                new PortalErrorDto("The current password is incorrect."),
                statusCode: StatusCodes.Status401Unauthorized
            );
        if (user.VerifyPassword(request.NewPassword))
            return TypedResults.BadRequest(
                new PortalErrorDto("The new password must be different from the current password.")
            );

        await users.UpdatePasswordAsync(userId, request.NewPassword);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> DisconnectAsync(
        int userId,
        ServerType serverType,
        ServerTypeSessionService sessions,
        CancellationToken ct
    ) =>
        TypedResults.Ok(
            new PortalDisconnectResultDto(
                await sessions.DisconnectUserAsync(userId, serverType, ct)
            )
        );

    private static async Task<IResult> GetAccountAsync(
        int userId,
        MainContext db,
        CancellationToken ct
    )
    {
        var user = await db
            .Users.AsNoTracking()
            .Include(u => u.StorageItems)
                .ThenInclude(item => item.Item)
            .Include(u => u.Characters)
                .ThenInclude(character => character.Inventory)
                    .ThenInclude(item => item.Item)
            .Include(u => u.Characters)
                .ThenInclude(character => character.Equipment)
                    .ThenInclude(item => item.Item)
            .Include(u => u.Characters)
                .ThenInclude(character => character.Robos)
                    .ThenInclude(robo => robo.Equipment)
            .SingleOrDefaultAsync(user => user.Id == userId, ct);
        if (user is null)
            return TypedResults.NotFound(new PortalErrorDto("User not found."));

        var mapIds = user
            .Characters.Select(character => (long)character.CurrentMapId)
            .Where(mapId => mapId != 0)
            .Distinct()
            .ToArray();
        var mapNames = await db
            .Maps.AsNoTracking()
            .Where(map => mapIds.Contains(map.MapId))
            .ToDictionaryAsync(map => map.MapId, map => map.Name, ct);
        var roboItemIds = user
            .Characters.SelectMany(character => character.Robos)
            .SelectMany(robo => robo.Equipment)
            .Select(item => (int)item.ItemId)
            .Where(itemId => itemId != 0)
            .Distinct()
            .ToArray();
        var roboItems = await db
            .Items.AsNoTracking()
            .Where(item => roboItemIds.Contains(item.Id))
            .ToDictionaryAsync(
                item => item.Id,
                item => (item.Name, item.Socket, item.IconId),
                ct
            );
        return TypedResults.Ok(MapAccount(user, mapNames, roboItems));
    }

    private static async Task<IResult> GetSummariesAsync(
        PortalUserIdsRequest request,
        MainContext db,
        CancellationToken ct
    )
    {
        var ids = request.UserIds.Distinct().Take(100).ToArray();
        if (ids.Length == 0)
            return TypedResults.BadRequest(new PortalErrorDto("At least one user ID is required."));
        var users = await db
            .Users.AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .Include(user => user.Characters)
                .ThenInclude(character => character.Robos)
            .ToListAsync(ct);
        return TypedResults.Ok<IReadOnlyList<PortalCharacterRoboSummaryDto>>(
            users
                .Select(user => new PortalCharacterRoboSummaryDto(
                    user.Id,
                    user.Characters.Select(character => new PortalCharacterRoboEntryDto(
                            character.Id,
                            character.Name,
                            character.Robos.Count
                        ))
                        .ToArray()
                ))
                .ToArray()
        );
    }

    private static PortalUserSummaryDto MapSummary(User user) =>
        new(
            user.Id,
            user.Username,
            user.IsBanned,
            user.CreatedAt,
            user.Characters.Count,
            user.Characters.Select(character => character.Name).ToArray()
        );

    private static PortalUserDetailDto MapDetail(User user) =>
        new(
            user.Id,
            user.Username,
            user.IsBanned,
            user.BanReason,
            user.CreatedAt,
            user.LastLoggedInAt,
            user.BannedAt
        );

    private static PortalAccountDataDto MapAccount(
        User user,
        IReadOnlyDictionary<long, string> mapNames,
        IReadOnlyDictionary<int, (string Name, int Socket, int IconId)> roboItems
    ) =>
        new(
            user.Id,
            user.Username,
            user.CreatedAt,
            user.LastLoggedInAt,
            user.AiPoints,
            user.NicoPoints,
            user.StorageDeposit,
            user.StorageItems.OrderBy(item => item.ItemId)
                .Select(item => new PortalItemDto(
                    item.ItemId,
                    item.Item.Name,
                    item.Item.Socket,
                    item.Item.IconId,
                    item.Quantity
                ))
                .ToArray(),
            user.Characters.OrderBy(character => character.Id)
                .Select(character => new PortalCharacterDto(
                    character.Id,
                    character.Name,
                    character.ModelId,
                    character.Birthdate,
                    character.CreatedAt,
                    character.LastLoggedInAt,
                    character.BloodType.ToString(),
                    character.AvatarDesc,
                    character.Like1,
                    character.LikeDesc1,
                    character.Like2,
                    character.LikeDesc2,
                    character.Like3,
                    character.LikeDesc3,
                    character.CurrentMapId,
                    mapNames.GetValueOrDefault(
                        (long)character.CurrentMapId,
                        character.CurrentMapId == 0
                            ? "No current map"
                            : $"Map {character.CurrentMapId}"
                    ),
                    character.HomeIslandId,
                    ResolveHomeIslandName(character.HomeIslandId),
                    character
                        .Inventory.OrderBy(item => item.ItemId)
                        .Select(item => new PortalItemDto(
                            item.ItemId,
                            item.Item.Name,
                            item.Item.Socket,
                            item.Item.IconId,
                            item.Quantity
                        ))
                        .ToArray(),
                    character
                        .Equipment.OrderBy(item => item.SlotIndex)
                        .Select(item => new PortalCharacterEquipmentDto(
                            item.SlotIndex,
                            ResolveEquipmentSlotName(item.SlotIndex),
                            item.ItemId,
                            item.Item.Name,
                            item.Item.Socket,
                            item.Item.IconId
                        ))
                        .ToArray(),
                    character
                        .Robos.OrderBy(robo => robo.RoboId)
                        .Select(robo => new PortalRoboDto(
                            robo.RoboId,
                            robo.Name,
                            robo.ModelId,
                            ResolvePersonalityName(character.CharadollPersonality),
                            robo.Level,
                            robo.Experience,
                            robo.ExperienceToNextLevel,
                            robo.StatusPoints,
                            robo.AvailableStatusPoints,
                            robo.Equipment.Where(item => item.ItemId != 0)
                                .OrderBy(item => item.SlotIndex)
                                .Select(item =>
                                {
                                    var itemId = (int)item.ItemId;
                                    var catalog = roboItems.GetValueOrDefault(itemId);
                                    return new PortalRoboEquipmentDto(
                                        item.SlotIndex,
                                        ResolveEquipmentSlotName(item.SlotIndex),
                                        itemId,
                                        string.IsNullOrWhiteSpace(catalog.Name)
                                            ? $"Item {itemId}"
                                            : catalog.Name,
                                        catalog.Socket != 0 ? catalog.Socket : (int)item.Socket,
                                        catalog.IconId != 0 ? catalog.IconId : itemId
                                    );
                                })
                                .ToArray()
                        ))
                        .ToArray()
                ))
                .ToArray()
        );

    private static string ResolveHomeIslandName(uint homeIslandId) =>
        homeIslandId switch
        {
            0 => "Not selected",
            1 => "Da Capo",
            2 => "Clannad",
            3 => "Shuffle",
            _ => $"Island {homeIslandId}",
        };

    private static string ResolvePersonalityName(CharadollPersonality personality) =>
        personality switch
        {
            CharadollPersonality.Active => "Active",
            CharadollPersonality.Quiet => "Quiet",
            CharadollPersonality.None => "No preference",
            _ => personality.ToString(),
        };

    private static string ResolveEquipmentSlotName(byte slotIndex) =>
        slotIndex switch
        {
            0 => "Top",
            1 => "Bottom",
            2 => "Socks",
            3 => "Shoes",
            4 => "Underwear",
            5 => "Bra",
            6 => "Hat",
            7 => "Gloves",
            8 => "Coat",
            9 => "Jacket",
            _ => "Accessory",
        };

    private static bool IsValidUsername(string username) =>
        username.Length is >= 3 and <= 64
        && username.All(character =>
            char.IsAsciiLetterOrDigit(character) || character is '_' or '.' or '-'
        );

    private static bool IsValidPassword(string password, string confirmPassword) =>
        !string.IsNullOrWhiteSpace(password)
        && password.Length is >= 8 and <= 128
        && password == confirmPassword;
}
