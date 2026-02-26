using AISpace.Common.DAL.Entities;

namespace AISpace.Common.DAL.Repositories;

public interface IUserRepository
{
    Task<User?> AuthenticateAsync(string username, string password);
    Task AddAsync(string username, string password);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetById(int userId);
}