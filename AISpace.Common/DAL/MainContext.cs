using AISpace.Common.Config;
using AISpace.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL;

public class MainContext(DbContextOptions<MainContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<GameChannel> Channels { get; set; }

    //public DbSet<ServerInformation> Servers { get; set; }
    public DbSet<World> Worlds { get; set; }
    public DbSet<Character> Characters => Set<Character>();

    public DbSet<Item> Items { get; set; }
    public DbSet<CharacterInventory> CharacterInventories { get; set; }
    public DbSet<CharacterEquipment> CharacterEquipments { get; set; }
    public DbSet<Circle> Circles { get; internal set; }
    public DbSet<Map> Maps { get; set; }
    public DbSet<MapLink> MapLinks { get; set; }
    public DbSet<Npc> Npcs { get; set; }
    public DbSet<NpcEquipment> NpcEquipments { get; set; }
    public DbSet<Shop> Shops { get; set; }
    public DbSet<ShopItem> ShopItems { get; set; }
    public DbSet<SessionPresence> SessionPresences { get; set; }
    public DbSet<PendingMapTransfer> PendingMapTransfers { get; set; }

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
            e.Property(x => x.PasswordHash).HasColumnName("PasswordHash").HasMaxLength(512).IsRequired();
            e.Property(x => x.AiPoints).HasDefaultValue(0L);
            e.Property(x => x.NicoPoints).HasDefaultValue(0L);
            e.Property(x => x.IsBanned).HasDefaultValue(false);
            e.Property(x => x.BanReason).HasMaxLength(256);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasIndex(x => x.Username).IsUnique();

            e.HasMany(x => x.Sessions).WithOne(s => s.User).HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Character>(e =>
        {
            e.ToTable("Characters");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();

            e.HasOne(x => x.User).WithMany(u => u.Characters).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Item
        b.Entity<Item>(e =>
        {
            e.ToTable("Items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Socket).HasDefaultValue(0);
            e.Property(x => x.IconId).HasDefaultValue(1);
        });

        b.Entity<CharacterInventory>(e =>
        {
            e.ToTable("CharacterInventory");
            e.HasKey(x => new { x.CharacterId, x.ItemId });

            e.HasOne(x => x.Character).WithMany(c => c.Inventory).HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);

            e.Property(x => x.Quantity).HasDefaultValue(1);
        });

        b.Entity<CharacterEquipment>(e =>
        {
            e.ToTable("CharacterEquipment");
            e.HasKey(x => new { x.CharacterId, x.SlotIndex });

            e.HasOne(x => x.Character).WithMany(c => c.Equipment).HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
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
            e.HasOne(x => x.Shop).WithMany(x => x.Items).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Cascade);
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
            e.HasIndex(x => x.NpcObjectId).IsUnique();
            e.HasIndex(x => new { x.MapId, x.SortOrder });
            e.HasOne(x => x.Shop).WithMany(x => x.Npcs).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<NpcEquipment>(e =>
        {
            e.ToTable("NpcEquipment");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.NpcId, x.SlotIndex }).IsUnique();
            e.HasOne(x => x.Npc).WithMany(x => x.Equipment).HasForeignKey(x => x.NpcId).OnDelete(DeleteBehavior.Cascade);
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
    }
}
