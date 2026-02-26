using AISpace.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public interface IUserRepository
{
    Task<User?> AuthenticateAsync(string username, string password);
    Task AddAsync(string username, string password);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetById(int userId);
}

public class UserRepository(MainContext db) : IUserRepository
{
    private readonly MainContext _db = db;

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var user = await _db.Users.Include(u => u.Characters).ThenInclude(c => c.Inventory).ThenInclude(i => i.Item).Include(u => u.Characters).ThenInclude(c => c.Equipment).ThenInclude(e => e.Item).SingleOrDefaultAsync(u => u.Username == username);
        if (user is null)
            return null;

        return user.VerifyPassword(password) ? user : null;
    }

    public async Task AddAsync(string username, string password)
    {
        var user = new User { Username = username };
        user.SetPassword(password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _db.Users.Include(u => u.Characters).ThenInclude(c => c.Inventory).ThenInclude(i => i.Item).Include(u => u.Characters).ThenInclude(c => c.Equipment).ThenInclude(e => e.Item).FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetById(int userId)
    {
        return await _db.Users.Include(u => u.Characters).ThenInclude(c => c.Inventory).ThenInclude(i => i.Item).Include(u => u.Characters).ThenInclude(c => c.Equipment).ThenInclude(e => e.Item).FirstOrDefaultAsync(u => u.Id == userId);
    }
}
