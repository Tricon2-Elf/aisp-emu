using AISpace.Common.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public class UserRepository(MainContext db) : IUserRepository
{

    private readonly MainContext _db = db;
    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        return await _db.Users
            .AnyAsync(u => u.Username == username && u.Password == password);
    }

    public async Task AddUserAsync(string username, string password)
    {
        if ((await GetUserByUsernameAsync(username)) != null)
            return;

        var user = new User { Username = username, Password = password };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task AddSessionAsync(uint userId, string otp)
    {
        var session = new UserSession { UserID = userId, OTP = otp };
        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync();
    }
    public async Task<bool> ValidateSessionAsync(int userId, string otp)
    {
        return await _db.UserSessions
            .AnyAsync(s => s.Id == userId && s.OTP == otp);
    }

    public async Task<UserSession?> GetSessionAsync(int userId, string otp)
    {
        return await _db.UserSessions.FirstOrDefaultAsync(s => s.Id == userId && s.OTP == otp);
    }
}
