using aisp.Common.Config;
using aisp.Common.DAL.Entities;
using aisp.Common.Game;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Data;
using Microsoft.EntityFrameworkCore;

namespace aisp.Common.DAL;

public class MainContext(DbContextOptions<MainContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<GameChannel> Channels => Set<GameChannel>();
    public DbSet<World> Worlds => Set<World>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Furniture> Furniture => Set<Furniture>();
    public DbSet<CharacterInventory> CharacterInventories => Set<CharacterInventory>();
    public DbSet<CharacterEquipment> CharacterEquipments => Set<CharacterEquipment>();
    public DbSet<UserStorageItem> UserStorageItems => Set<UserStorageItem>();
    public DbSet<Robo> Robos => Set<Robo>();
    public DbSet<RoboTpsBattleData> RoboTpsBattleData => Set<RoboTpsBattleData>();
    public DbSet<RoboEquipment> RoboEquipment => Set<RoboEquipment>();
    public DbSet<RoboItemUseEffect> RoboItemUseEffects => Set<RoboItemUseEffect>();
    public DbSet<RoboBattleAbility> RoboBattleAbilities => Set<RoboBattleAbility>();
    public DbSet<RoboDistributedStatusPoint> RoboDistributedStatusPoints =>
        Set<RoboDistributedStatusPoint>();
    public DbSet<CharacterEventStatus> CharacterEventStatuses => Set<CharacterEventStatus>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<MyRoomFurniture> MyRoomFurniture => Set<MyRoomFurniture>();
    public DbSet<Nicotv> Nicotvs => Set<Nicotv>();
    public DbSet<Circle> Circles => Set<Circle>();
    public DbSet<CircleMember> CircleMembers => Set<CircleMember>();
    public DbSet<CircleJoinRequest> CircleJoinRequests => Set<CircleJoinRequest>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<AdventureWork> AdventureWorks => Set<AdventureWork>();
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
    public DbSet<Map> Maps => Set<Map>();
    public DbSet<MapLink> MapLinks => Set<MapLink>();
    public DbSet<Npc> Npcs => Set<Npc>();
    public DbSet<NpcEquipment> NpcEquipments => Set<NpcEquipment>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<ShopItem> ShopItems => Set<ShopItem>();
    public DbSet<SessionPresence> SessionPresences => Set<SessionPresence>();
    public DbSet<PendingMapTransfer> PendingMapTransfers => Set<PendingMapTransfer>();
    public DbSet<LocalisedText> LocalisedTexts => Set<LocalisedText>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ReportTicket> ReportTickets => Set<ReportTicket>();
    public DbSet<ReportTicketPlayer> ReportTicketPlayers => Set<ReportTicketPlayer>();
    public DbSet<ReportTicketChatMessage> ReportTicketChatMessages => Set<ReportTicketChatMessage>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            new DbOptions().ConfigureDbContext(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(64).IsRequired();
            e.Property(x => x.PasswordHash)
                .HasColumnName("PasswordHash")
                .HasMaxLength(512)
                .IsRequired();
            e.Property(x => x.AiPoints).HasDefaultValue(0L);
            e.Property(x => x.NicoPoints).HasDefaultValue(0L);
            e.Property(x => x.StorageDeposit).HasDefaultValue(0L);
            e.Property(x => x.Role)
                .HasConversion<byte>()
                .HasDefaultValue(UserRole.User)
                .HasSentinel(UserRole.User);
            e.Property(x => x.IsBanned).HasDefaultValue(false);
            e.Property(x => x.AdventureSheetStock).HasDefaultValue(0);
            e.Property(x => x.NextAdventureWorkId).HasDefaultValue(1);
            e.Property(x => x.BanReason).HasMaxLength(256);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(x => x.Language)
                .HasColumnName("PreferredLanguage")
                .HasConversion<byte>()
                .HasDefaultValue(GameLanguage.Japanese)
                .HasSentinel(GameLanguage.Japanese);
            e.HasIndex(x => x.Username).IsUnique();

            e.HasMany(x => x.Sessions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Character>(e =>
        {
            e.ToTable("Characters");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.CharadollPersonality)
                .HasConversion<byte>()
                .HasDefaultValue(CharadollPersonality.None)
                .HasSentinel(CharadollPersonality.None);

            e.HasOne(x => x.User)
                .WithMany(u => u.Characters)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CurrentRoom)
                .WithMany()
                .HasForeignKey(x => x.CurrentRoomId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Room>(e =>
        {
            e.ToTable("Rooms");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(45).IsRequired().HasDefaultValue("My Room");
            e.Property(x => x.Stage).HasConversion<byte>().HasDefaultValue(MyRoomStage.SixTatami);
            e.Property(x => x.Security)
                .HasConversion<uint>()
                .HasDefaultValue(MyRoomSecurity.Private);
            e.Property(x => x.IsDefault).HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasOne(x => x.OwnerCharacter)
                .WithMany(x => x.Rooms)
                .HasForeignKey(x => x.OwnerCharacterId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.OwnerCharacterId, x.IsDefault });
        });

        b.Entity<MyRoomFurniture>(e =>
        {
            e.ToTable("MyRoomFurniture");
            e.HasKey(x => new { x.RoomId, x.FurnitureId });
            e.HasOne(x => x.Room)
                .WithMany(x => x.Furniture)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Furniture>()
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Nicotv>(e =>
        {
            e.ToTable("Nicotvs");
            e.HasKey(x => x.Id);
            e.Property(x => x.MovieId).HasMaxLength(96).IsRequired().HasDefaultValue("");
            e.Property(x => x.PlaybackState)
                .HasConversion<uint>()
                .HasDefaultValue(NicotvPlaybackState.Closed);
            e.Property(x => x.CommentVisibility)
                .HasConversion<uint>()
                .HasDefaultValue(NicotvCommentVisibility.Visible);
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasIndex(x => new { x.RoomId, x.FurnitureId }).IsUnique();
            e.HasOne(x => x.Furniture)
                .WithOne(x => x.Nicotv)
                .HasForeignKey<Nicotv>(x => new { x.RoomId, x.FurnitureId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CharacterEventStatus>(e =>
        {
            e.ToTable("CharacterEventStatuses");
            e.HasKey(x => new { x.CharacterId, x.EventKey });
            e.Property(x => x.EventKey).HasMaxLength(128).IsRequired();
            e.HasOne(x => x.Character)
                .WithMany()
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.CharacterId);
        });

        // Item
        b.Entity<Item>(e =>
        {
            e.ToTable("Items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Socket).HasDefaultValue(0);
            e.Property(x => x.IconId).HasDefaultValue(1);
            e.Property(x => x.CatalogCategory);
        });

        b.Entity<LocalisedText>(e =>
        {
            e.ToTable("LocalisedTexts");
            e.HasKey(x => new { x.Key, x.Language });
            e.Property(x => x.Key).HasMaxLength(128).IsRequired();
            e.Property(x => x.Language).HasConversion<byte>();
            e.Property(x => x.Value).IsRequired();
            e.HasIndex(x => x.Key);
        });

        b.Entity<Furniture>(e =>
        {
            e.ToTable("Furniture");
            e.HasKey(x => x.ItemId);
            e.Property(x => x.PlacementFlags).HasConversion<uint>();
            e.HasOne(x => x.Item)
                .WithOne(x => x.Furniture)
                .HasForeignKey<Furniture>(x => x.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CharacterInventory>(e =>
        {
            e.ToTable("CharacterInventory");
            e.HasKey(x => new { x.CharacterId, x.ItemId });

            e.HasOne(x => x.Character)
                .WithMany(c => c.Inventory)
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Property(x => x.Quantity).HasDefaultValue(1);
        });

        b.Entity<UserStorageItem>(e =>
        {
            e.ToTable("UserStorageItems");
            e.HasKey(x => new { x.UserId, x.ItemId });

            e.HasOne(x => x.User)
                .WithMany(u => u.StorageItems)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Item)
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(x => x.Quantity).HasDefaultValue(1);
        });

        b.Entity<CharacterEquipment>(e =>
        {
            e.ToTable("CharacterEquipment");
            e.HasKey(x => new { x.CharacterId, x.SlotIndex });

            e.HasOne(x => x.Character)
                .WithMany(c => c.Equipment)
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Robo>(e =>
        {
            e.ToTable("Robos");
            e.HasKey(x => new { x.CharacterId, x.RoboId });
            e.Property(x => x.Name).HasMaxLength(37).IsRequired();
            e.Property(x => x.BloodType).HasConversion<uint>();
            e.Property(x => x.UserStatusText)
                .HasMaxLength(UserStatusData.StatusTextLength)
                .IsRequired();
            e.Property(x => x.Like1).HasDefaultValue(string.Empty).IsRequired();
            e.Property(x => x.Like2).HasDefaultValue(string.Empty).IsRequired();
            e.Property(x => x.Like3).HasDefaultValue(string.Empty).IsRequired();
            e.Property(x => x.LikeDesc1).HasDefaultValue(string.Empty).IsRequired();
            e.Property(x => x.LikeDesc2).HasDefaultValue(string.Empty).IsRequired();
            e.Property(x => x.LikeDesc3).HasDefaultValue(string.Empty).IsRequired();
            e.Property(x => x.ProfileDescription).HasDefaultValue(string.Empty).IsRequired();
            e.Property(x => x.ProfileUnknownDword04).HasDefaultValue(0u);
            e.Property(x => x.ProfileUnknownDword08).HasDefaultValue(0u);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasOne(x => x.Character)
                .WithMany(c => c.Robos)
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TpsBattleData)
                .WithOne(x => x.Robo)
                .HasForeignKey<RoboTpsBattleData>(x => new { x.CharacterId, x.RoboId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RoboTpsBattleData>(e =>
        {
            e.ToTable("RoboTpsBattleData");
            e.HasKey(x => new { x.CharacterId, x.RoboId });
        });

        b.Entity<RoboEquipment>(e =>
        {
            e.ToTable("RoboEquipment");
            e.HasKey(x => new
            {
                x.CharacterId,
                x.RoboId,
                x.SlotIndex,
            });
            e.HasOne(x => x.Robo)
                .WithMany(x => x.Equipment)
                .HasForeignKey(x => new { x.CharacterId, x.RoboId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RoboItemUseEffect>(e =>
        {
            e.ToTable("RoboItemUseEffects");
            e.HasKey(x => new
            {
                x.CharacterId,
                x.RoboId,
                x.SlotIndex,
            });
            e.HasOne(x => x.Robo)
                .WithMany(x => x.ItemUseEffects)
                .HasForeignKey(x => new { x.CharacterId, x.RoboId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RoboBattleAbility>(e =>
        {
            e.ToTable("RoboBattleAbilities");
            e.HasKey(x => new
            {
                x.CharacterId,
                x.RoboId,
                x.AbilitySet,
                x.AbilityIndex,
            });
            e.Property(x => x.AbilitySet).HasConversion<byte>();
            e.HasOne(x => x.TpsBattleData)
                .WithMany(x => x.BattleAbilities)
                .HasForeignKey(x => new { x.CharacterId, x.RoboId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RoboDistributedStatusPoint>(e =>
        {
            e.ToTable("RoboDistributedStatusPoints");
            e.HasKey(x => new
            {
                x.CharacterId,
                x.RoboId,
                x.StatusIndex,
            });
            e.HasOne(x => x.Robo)
                .WithMany(x => x.DistributedStatusPoints)
                .HasForeignKey(x => new { x.CharacterId, x.RoboId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<UserSession>(e =>
        {
            e.ToTable("UserSessions");
            e.HasKey(x => x.Id);

            e.Property(x => x.OTP).HasMaxLength(16).IsRequired();

            e.Property(x => x.ExpiresAt).IsRequired();

            e.HasIndex(x => new { x.UserId, x.OTP }).IsUnique();
        });

        b.Entity<Circle>(e =>
        {
            e.ToTable("Circles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(46).IsRequired();
            e.Property(x => x.Mark).HasMaxLength(37).HasDefaultValue(string.Empty);
            e.Property(x => x.Message).HasMaxLength(751).HasDefaultValue(string.Empty);
            e.Property(x => x.MessageDate).HasMaxLength(20).HasDefaultValue(string.Empty);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasOne(x => x.LeaderCharacter)
                .WithMany()
                .HasForeignKey(x => x.LeaderCharacterId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.Name);
        });

        b.Entity<CircleMember>(e =>
        {
            e.ToTable("CircleMembers");
            e.HasKey(x => new { x.CircleId, x.CharacterId });
            e.Property(x => x.JoinedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasOne(x => x.Circle)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.CircleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Character)
                .WithMany(x => x.CircleMemberships)
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.CharacterId);
        });

        b.Entity<CircleJoinRequest>(e =>
        {
            e.ToTable("CircleJoinRequests");
            e.HasKey(x => x.Id);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasOne(x => x.Circle)
                .WithMany(x => x.JoinRequests)
                .HasForeignKey(x => x.CircleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.RequesterCharacter)
                .WithMany()
                .HasForeignKey(x => x.RequesterCharacterId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TargetCharacter)
                .WithMany()
                .HasForeignKey(x => x.TargetCharacterId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TargetCharacterId, x.Status });
            e.HasIndex(x => new { x.RequesterCharacterId, x.Status });
            e.HasIndex(x => new
            {
                x.CircleId,
                x.TargetCharacterId,
                x.Status,
            });
        });

        b.Entity<AdventureWork>(e =>
        {
            e.ToTable("AdventureWorks");
            e.HasKey(x => x.Id);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UserId, x.WorkId }).IsUnique();
        });

        b.Entity<Friendship>(e =>
        {
            e.ToTable("Friendships");
            e.HasKey(x => new { x.CharacterIdLow, x.CharacterIdHigh });
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasOne(x => x.CharacterLow)
                .WithMany()
                .HasForeignKey(x => x.CharacterIdLow)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CharacterHigh)
                .WithMany()
                .HasForeignKey(x => x.CharacterIdHigh)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.CharacterIdHigh);
        });

        b.Entity<FriendRequest>(e =>
        {
            e.ToTable("FriendRequests");
            e.HasKey(x => x.Id);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasOne(x => x.RequesterCharacter)
                .WithMany()
                .HasForeignKey(x => x.RequesterCharacterId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TargetCharacter)
                .WithMany()
                .HasForeignKey(x => x.TargetCharacterId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TargetCharacterId, x.Status });
            e.HasIndex(x => new { x.RequesterCharacterId, x.Status });
        });

        b.Entity<Map>(e =>
        {
            e.ToTable("Maps");
            e.HasKey(x => x.MapId);
            e.Property(x => x.Island).HasMaxLength(32).IsRequired();
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
        });

        b.Entity<MapLink>(e =>
        {
            e.ToTable("MapLinks");
            e.HasKey(x => x.Id);
            e.Property(x => x.DestinationMapIds).HasMaxLength(256).IsRequired();
            e.HasIndex(x => new
            {
                x.SourceMapId,
                x.ChannelId,
                x.SortOrder,
            });
        });

        b.Entity<Shop>(e =>
        {
            e.ToTable("Shops");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(128).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(128).IsRequired();
            e.Property(x => x.IsEnabled).HasDefaultValue(true);
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<ShopItem>(e =>
        {
            e.ToTable("ShopItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.AiPrice).HasDefaultValue(0L);
            e.Property(x => x.NicoPrice).HasDefaultValue(0L);
            e.Property(x => x.IsEnabled).HasDefaultValue(true);
            e.HasIndex(x => new { x.ShopId, x.ItemId }).IsUnique();
            e.HasIndex(x => new { x.ShopId, x.SortOrder });
            e.HasOne(x => x.Shop)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.ShopId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Npc>(e =>
        {
            e.ToTable("Npcs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.ChannelId).HasDefaultValue(-1);
            e.Property(x => x.DayPhase).HasDefaultValue(-1);
            e.Property(x => x.DateStartUtc).HasDefaultValue(DateTime.UnixEpoch);
            e.Property(x => x.DateEndUtc).HasDefaultValue(DateTime.MaxValue);
            e.Property(x => x.InteractionType).HasConversion<int>();
            e.Property(x => x.IsEnabled).HasDefaultValue(true);
            e.Property(x => x.EventKind).HasConversion<int>().HasDefaultValue(NpcEventKind.None);
            e.Property(x => x.EventKey).HasMaxLength(128);
            e.HasIndex(x => x.NpcObjectId).IsUnique();
            e.HasIndex(x => new { x.MapId, x.SortOrder });
            e.HasOne(x => x.Shop)
                .WithMany(x => x.Npcs)
                .HasForeignKey(x => x.ShopId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<NpcEquipment>(e =>
        {
            e.ToTable("NpcEquipment");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.NpcId, x.SlotIndex }).IsUnique();
            e.HasOne(x => x.Npc)
                .WithMany(x => x.Equipment)
                .HasForeignKey(x => x.NpcId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<GameChannel>(e =>
        {
            e.ToTable("Channels");
            e.HasKey(x => x.Id);
            e.Property(x => x.IP).HasMaxLength(256).IsRequired();
            e.Property(x => x.MaxUsers).HasDefaultValue(1000u);
            e.Property(x => x.MapId).HasDefaultValue(10990100u);
        });

        b.Entity<SessionPresence>(e =>
        {
            e.ToTable("SessionPresences");
            e.HasKey(x => x.ConnectionId);
            e.HasIndex(x => new { x.ServerType, x.UserId });
            e.HasIndex(x => new { x.ServerType, x.CharacterId });
            e.HasIndex(x => new
            {
                x.ServerType,
                x.MapId,
                x.ChannelId,
            });
            e.HasIndex(x => x.UpdatedAtUtc);
        });

        b.Entity<PendingMapTransfer>(e =>
        {
            e.ToTable("PendingMapTransfers");
            e.HasKey(x => x.UserId);
            e.HasIndex(x => x.ExpiresAtUtc);
        });

        b.Entity<ChatMessage>(e =>
        {
            e.ToTable("ChatMessages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Kind).HasConversion<byte>();
            e.Property(x => x.CharacterName).HasMaxLength(128).IsRequired();
            e.Property(x => x.Message).HasMaxLength(1024).IsRequired();
            e.Property(x => x.Rejected).HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => new { x.CharacterId, x.CreatedAt });
            e.HasIndex(x => new { x.UserId, x.CreatedAt });
            e.HasIndex(x => new { x.Kind, x.CreatedAt });
            e.HasIndex(x => new { x.CircleId, x.CreatedAt });
            e.HasIndex(x => new { x.MapId, x.ChannelId, x.CreatedAt });
        });

        b.Entity<ReportTicket>(e =>
        {
            e.ToTable("ReportTickets");
            e.HasKey(x => x.Id);
            e.Property(x => x.ReporterUsername).HasMaxLength(64).IsRequired();
            e.Property(x => x.ReporterCharacterName).HasMaxLength(128).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(1024).IsRequired();
            e.Property(x => x.MapName).HasMaxLength(128).IsRequired();
            e.Property(x => x.ResolutionAction).HasMaxLength(1024);
            e.Property(x => x.Status).HasConversion<byte>().HasDefaultValue(ReportTicketStatus.Open);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => new { x.Status, x.CreatedAt });
        });

        b.Entity<ReportTicketPlayer>(e =>
        {
            e.ToTable("ReportTicketPlayers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(64).IsRequired();
            e.Property(x => x.CharacterName).HasMaxLength(128).IsRequired();
            e.HasOne(x => x.ReportTicket)
                .WithMany(x => x.Players)
                .HasForeignKey(x => x.ReportTicketId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ReportTicketId);
        });

        b.Entity<ReportTicketChatMessage>(e =>
        {
            e.ToTable("ReportTicketChatMessages");
            e.HasKey(x => x.Id);
            e.Property(x => x.CharacterName).HasMaxLength(128).IsRequired();
            e.Property(x => x.Message).HasMaxLength(1024).IsRequired();
            e.HasOne(x => x.ReportTicket)
                .WithMany(x => x.ChatMessages)
                .HasForeignKey(x => x.ReportTicketId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ReportTicketId);
            e.HasIndex(x => new { x.ReportTicketId, x.CreatedAt });
        });
    }
}
