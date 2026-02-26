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

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=main.db");
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(64).IsRequired();
            e.Property(x => x.PasswordHash).HasColumnName("PasswordHash").HasMaxLength(512).IsRequired();
            e.Property(x => x.NpsPoints).HasDefaultValue(0L);
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
    }
}
