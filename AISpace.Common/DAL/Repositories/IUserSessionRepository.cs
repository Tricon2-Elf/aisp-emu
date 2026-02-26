using AISpace.Common.DAL.Entities; // Обязательно для UserSession
using System.Threading;
using System.Threading.Tasks;

namespace AISpace.Common.DAL.Repositories;

public interface IUserSessionRepository
{
    Task<UserSession> CreateAsync(int userId, string otp, TimeSpan duration, CancellationToken ct = default);
    Task<UserSession?> GetValidSessionAsync(string otp, CancellationToken ct = default);
    Task InvalidateExpiredAsync(CancellationToken ct = default);
    Task DeleteAllForUserAsync(int userId, CancellationToken ct = default);
}