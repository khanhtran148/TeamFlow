using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface IAuthService
{
    string GenerateJwt(User user);
    string GenerateRefreshToken();
    string HashToken(string token);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
