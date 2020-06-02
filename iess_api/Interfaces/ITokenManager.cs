using iess_api.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace iess_api.Interfaces
{
    public interface ITokenManager
    {
        Task<JsonWebTokenModel> RefreshTokenAsync(string refreshTokenId, UserModel person, JsonWebTokenModel newToken);
        Task<bool> RevokeRefreshTokenAsync(RefreshTokenModel refreshToken);
        Task<string> GetIdFromToken(string token);
        Task<UserModel> GetPersonByNameAsync(string username);
        List<Claim> GetClaimsAsync(UserModel person);
        JsonWebTokenModel GetTokenAsync(List<Claim> claims);
        Task<bool> DeletePreviousRefreshTokenAsync(string username);
        Task<RefreshTokenModel> SetNewRefreshTokenAsync(string username, string refreshTokenString);
        Task<RefreshTokenModel> GetRefreshToken(string token);
        Task<RefreshTokenModel> GetRefreshTokenByUsername(string username);
    }
}