using aisp.Common;
using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Portal;
using aisp.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace aisp.Server;

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
        area.MapPost("/users/{userId:int}/language", SetPreferredLanguageAsync);
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
        IOptions<PortalOptions> portalOptions,
        CancellationToken ct
    )
    {
        if (!portalOptions.Value.AllowRegistration)
            return TypedResults.NotFound();

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
        bool? all,
        IUserRepository users,
        CancellationToken ct
    )
    {
        var returnAll = all == true;
        var actualSkip = returnAll ? 0 : Math.Max(skip ?? 0, 0);
        var actualTake = returnAll ? int.MaxValue : Math.Clamp(take ?? 50, 1, 100);
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
        ITextLocaliser localiser,
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
            .ToDictionaryAsync(item => item.Id, item => (item.Name, item.Socket, item.IconId), ct);
        return TypedResults.Ok(MapAccount(user, roboItems, localiser));
    }

    private static async Task<IResult> SetPreferredLanguageAsync(
        int userId,
        PortalChangeLanguageRequest request,
        IUserRepository users,
        CancellationToken ct
    )
    {
        if (!GameLanguages.TryParse(request.PreferredLanguage, out var language))
            return TypedResults.BadRequest(new PortalErrorDto("Unsupported language."));

        var user = await users.GetById(userId);
        if (user is null)
            return TypedResults.NotFound(new PortalErrorDto("User not found."));

        await users.SetLanguageAsync(userId, language, ct);
        return TypedResults.Ok(new PortalChangeLanguageRequest(language.ToTag()));
    }

    private static async Task<IResult> GetSummariesAsync(
        PortalUserIdsRequest request,
        MainContext db,
        SharedState state,
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
        var areaSessions = state
            .GetServerClients(ServerType.Area)
            .Where(session => session.IsAuthenticated && session.CharacterId != 0)
            .GroupBy(session => session.CharacterId)
            .ToDictionary(group => group.Key, group => group.First());
        var msgCharacterIds = state
            .GetServerClients(ServerType.Msg)
            .Where(session => session.IsAuthenticated && session.CharacterId != 0)
            .Select(session => session.CharacterId)
            .ToHashSet();
        var mapIds = areaSessions
            .Values.Select(session => (long)session.MapId)
            .Distinct()
            .ToArray();
        var maps = await db
            .Maps.AsNoTracking()
            .Where(map => mapIds.Contains(map.MapId))
            .ToDictionaryAsync(map => map.MapId, map => map.Name, ct);
        var roomIds = areaSessions
            .Values.Where(session => MyRoomInfo.IsMyRoomMap(session.MapId) && session.MyRoomId != 0)
            .Select(session => checked((int)session.MyRoomId))
            .Distinct()
            .ToArray();
        var rooms = await db
            .Rooms.AsNoTracking()
            .Where(room => roomIds.Contains(room.Id))
            .ToDictionaryAsync(room => room.Id, room => room.Name, ct);

        string ResolveLocation(int characterId)
        {
            if (!areaSessions.TryGetValue((uint)characterId, out var session))
                return msgCharacterIds.Contains((uint)characterId) ? "Character select" : "—";
            if (
                MyRoomInfo.IsMyRoomMap(session.MapId) && session.MyRoomId is > 0 and <= int.MaxValue
            )
            {
                var roomId = (int)session.MyRoomId;
                var roomName = rooms.GetValueOrDefault(roomId, "MYROOM");
                return $"{roomName}({roomId})";
            }

            return maps.GetValueOrDefault(session.MapId, $"Map {session.MapId}");
        }

        return TypedResults.Ok<IReadOnlyList<PortalCharacterRoboSummaryDto>>(
            users
                .Select(user => new PortalCharacterRoboSummaryDto(
                    user.Id,
                    user.Characters.Select(character => new PortalCharacterRoboEntryDto(
                            character.Id,
                            character.Name,
                            character.Robos.Count,
                            areaSessions.ContainsKey((uint)character.Id)
                                || msgCharacterIds.Contains((uint)character.Id),
                            ResolveLocation(character.Id)
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
        IReadOnlyDictionary<int, (string Name, int Socket, int IconId)> roboItems,
        ITextLocaliser localiser
    )
    {
        var language = user.Language;
        return new(
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
                    localiser.Get(language, L.Item.Name(item.ItemId)),
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
                    character.CurrentMapId == 0
                        ? localiser.Get(language, L.Map.NoCurrentMap)
                        : localiser.Get(language, L.Map.Name(character.CurrentMapId)),
                    character.HomeIslandId,
                    ResolveHomeIslandName(character.HomeIslandId, language, localiser),
                    character
                        .Inventory.OrderBy(item => item.ItemId)
                        .Select(item => new PortalItemDto(
                            item.ItemId,
                            localiser.Get(language, L.Item.Name(item.ItemId)),
                            item.Item.Socket,
                            item.Item.IconId,
                            item.Quantity
                        ))
                        .ToArray(),
                    character
                        .Equipment.OrderBy(item => item.SlotIndex)
                        .Select(item => new PortalCharacterEquipmentDto(
                            item.SlotIndex,
                            ResolveEquipmentSlotName(item.SlotIndex, language, localiser),
                            item.ItemId,
                            localiser.Get(language, L.Item.Name(item.ItemId)),
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
                            ResolvePersonalityName(
                                character.CharadollPersonality,
                                language,
                                localiser
                            ),
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
                                    var catalogName = string.IsNullOrWhiteSpace(catalog.Name)
                                        ? localiser.Get(
                                            language,
                                            L.Equipment.UnknownItemFormat,
                                            itemId
                                        )
                                        : localiser.Get(language, L.Item.Name(itemId));
                                    return new PortalRoboEquipmentDto(
                                        item.SlotIndex,
                                        ResolveEquipmentSlotName(
                                            item.SlotIndex,
                                            language,
                                            localiser
                                        ),
                                        itemId,
                                        catalogName,
                                        catalog.Socket != 0 ? catalog.Socket : (int)item.Socket,
                                        catalog.IconId != 0 ? catalog.IconId : itemId
                                    );
                                })
                                .ToArray()
                        ))
                        .ToArray()
                ))
                .ToArray(),
            language.ToTag()
        );
    }

    private static string ResolveHomeIslandName(
        uint homeIslandId,
        GameLanguage language,
        ITextLocaliser localiser
    ) =>
        homeIslandId == 0
            ? localiser.Get(language, L.Island.NotSelected)
            : localiser.Get(language, L.Island.Name(homeIslandId));

    private static string ResolvePersonalityName(
        CharadollPersonality personality,
        GameLanguage language,
        ITextLocaliser localiser
    ) =>
        personality switch
        {
            CharadollPersonality.Active => localiser.Get(language, L.Charadoll.PersonalityActive),
            CharadollPersonality.Quiet => localiser.Get(language, L.Charadoll.PersonalityQuiet),
            CharadollPersonality.None => localiser.Get(language, L.Charadoll.PersonalityNone),
            _ => personality.ToString(),
        };

    private static string ResolveEquipmentSlotName(
        byte slotIndex,
        GameLanguage language,
        ITextLocaliser localiser
    ) =>
        slotIndex <= 9
            ? localiser.Get(language, L.Equipment.Slot(slotIndex))
            : localiser.Get(language, L.Equipment.Accessory);

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
