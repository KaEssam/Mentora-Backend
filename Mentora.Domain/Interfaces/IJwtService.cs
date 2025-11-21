using System.Security.Claims;
using Mentora.Domain.Models;

namespace Mentora.Domain.Interfaces;

public interface IJwtService
{
    string GenerateToken(IUser user);
    ClaimsPrincipal? ValidateToken(string token);
    string GenerateRefreshToken();
}
