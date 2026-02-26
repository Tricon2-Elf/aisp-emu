using AISpace.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL;

public class MainContext(DbContextOptions<MainContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<GameChannel> Channels { get; set; }
    public DbSet<World> Worlds { get; set; }
    public DbSet<Character> Characters { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<Map> Maps { get; set; }
    public DbSet<CharacterInventory> CharacterInventories { get; set; }
    public DbSet<CharacterEquipment> CharacterEquipments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=main.db");
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasKey(x => x.Id);
        b.Entity<Character>().HasKey(x => x.Id);
        b.Entity<Map>().HasKey(x => x.Id);
        b.Entity<CharacterInventory>().HasKey(x => new { x.CharacterId, x.ItemId });
        b.Entity<CharacterEquipment>().HasKey(x => new { x.CharacterId, x.SlotIndex });

        b.Entity<Character>()
            .HasOne(x => x.User)
            .WithMany(u => u.Characters)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}