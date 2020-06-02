using iess_api.Interfaces;
using iess_api.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace iess_api.Managers
{
    public class TokenManager : ITokenManager
    {
        private readonly IConfiguration _configuration;
        
        private readonly IUserRepository _userRepository;

        private readonly ITokenRepository _tokenRepository;
        
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public TokenManager(ITokenRepository tokenRepository, IRefreshTokenRepository refreshTokenRepository, 
                            IUserRepository userRepository, IConfiguration configuration)
        {
            _tokenRepository = tokenRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public async Task<string> GetIdFromToken(string token)
        {
            var securityToken = new JwtSecurityToken(jwtEncodedString: token);
            string id = securityToken.Claims.First(c => c.Type == "subject").Value;
            return id;
        }
        
        public JsonWebTokenModel GetTokenAsync(List<Claim> claims)
        {
            var accessJwt = CreateToken(claims, 2);
            var refreshJwt = CreateToken(claims, 24 * 30);
            var accessJwtString = new JwtSecurityTokenHandler().WriteToken(accessJwt);
            var refreshJwtString = new JwtSecurityTokenHandler().WriteToken(refreshJwt);
            var response = new JsonWebTokenModel
            {
                AccessToken = accessJwtString,
                RefreshToken = refreshJwtString
            };
            return response;
        }

        public async Task<JsonWebTokenModel> RefreshTokenAsync(string refreshTokenId, UserModel person, JsonWebTokenModel newToken)
        {
            var refreshTokenModel = new RefreshTokenModel
            {
                Id = refreshTokenId,
                Username = person.Login,
                Token = newToken.RefreshToken,
                Revoked = false
            };
            var refreshTokenReplaceResult = await _refreshTokenRepository.ReplaceAsync(refreshTokenModel, x => x.Id == refreshTokenId);
            if (!refreshTokenReplaceResult)
            {
                return null;
            }
            await _userRepository.UpdateLastLogin(person.UserId);

            return newToken;
        }

        public async Task<bool> RevokeRefreshTokenAsync(RefreshTokenModel refreshToken)
        {
            refreshToken.Revoked = true;
            var result = await _refreshTokenRepository.ReplaceAsync(refreshToken, x => x.Token == refreshToken.Token);
            return result;
        }

        public JwtSecurityToken CreateToken(List<Claim> claims, int expiresTime)
        {
            var now = DateTime.UtcNow;

            var key = _configuration["JWT:key"];
            var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var jwt = new JwtSecurityToken(
                    issuer: _configuration["JWT:issuer"],
                    audience: _configuration["JWT:audience"],
                    notBefore: now,
                    claims: claims,
                    expires: now.Add(TimeSpan.FromHours(expiresTime)),
                    signingCredentials: new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256Signature));
            return jwt;
        }

        public List<Claim> GetClaimsAsync(UserModel person)
        {
            var claims = new List<Claim> { new Claim("subject", person.UserId) };

            foreach (var claim in _configuration.GetSection("JWT:roles:" + person.Role).Get<string[]>())
            {
                claims.Add(new Claim(ClaimsIdentity.DefaultRoleClaimType, claim));
            }
            return claims;
        }

        public async Task<UserModel> GetPersonByNameAsync(string username)
        {
            var person = await _tokenRepository.GetPersonByNameAsync(username);
            return person;
        }

        public async Task<bool> DeletePreviousRefreshTokenAsync(string username)
        {
            var deletePrevious = await _refreshTokenRepository.DeleteAsync(x => x.Username == username);
            return deletePrevious;
        }

        public async Task<RefreshTokenModel> SetNewRefreshTokenAsync(string username, string refreshTokenString)
        {
            var refreshTokenModel = new RefreshTokenModel
            {
                Username = username,
                Token = refreshTokenString,
                Revoked = false
            };
            var result = await _refreshTokenRepository.PostAsync(refreshTokenModel);
            return result;
        }

        public async Task<RefreshTokenModel> GetRefreshToken(string token)
        {
            var refreshToken = await _refreshTokenRepository.GetRefreshToken(token);
            return refreshToken;
        }

        public async Task<RefreshTokenModel> GetRefreshTokenByUsername(string username)
        {
            var refreshToken = await _refreshTokenRepository.GetRefreshTokenByUsername(username);
            return refreshToken;
        }
    }
}