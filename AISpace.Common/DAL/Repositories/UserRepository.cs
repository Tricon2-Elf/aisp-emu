using AISpace.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public class UserRepository(MainContext db) : IUserRepository
{
    private readonly MainContext _db = db;

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var user = await _db.Users
            .Include(u => u.Characters)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null) return null;

        if (user.VerifyPassword(password))
        {
            return user;
        }

        return null;
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
        return await _db.Users
            .Include(u => u.Characters)
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetById(int userId)
    {
        return await _db.Users
            .Include(u => u.Characters)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}
