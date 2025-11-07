using System.Security.Claims;
using Mentora.Core.Data;

namespace Mentora.Domain.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
    ClaimsPrincipal? ValidateToken(string token);
    string GenerateRefreshToken();
}