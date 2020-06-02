using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentValidation.AspNetCore;
using iess_api.Interfaces;
using iess_api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iess_api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    //todo add validation
    public class AccountController : ControllerBase
    {
        private readonly ITokenManager _tokenManager;
        public AccountController(ITokenManager tokenManager)
        {
            _tokenManager = tokenManager;
        }

        /// <summary>
        /// Get token by model.
        /// </summary>
        /// <param name="loginPerson"></param>
        /// <returns></returns>
        [Route("token")]
        [HttpPost]
        public async Task<IActionResult> CreateTokenAsync([FromBody]LoginModel loginPerson)
        {
            var username = loginPerson.Login;
            var password = loginPerson.Password;
            var person = await _tokenManager.GetPersonByNameAsync(username);
            
            if (person == null)
            {
                return NotFound("Invalid password or login");
            }
            
            if (!BCrypt.Net.BCrypt.Verify(password, person.PasswordHash))
            {
                return NotFound("Invalid password or login");
            }

            var claims = _tokenManager.GetClaimsAsync(person);
            var tokenModel = _tokenManager.GetTokenAsync(claims);

            var deletePrevious = await _tokenManager.DeletePreviousRefreshTokenAsync(username);
            if (!deletePrevious)
            {
                return BadRequest("Previous refresh token cannot be deleted");
            }

            var refreshPost = await _tokenManager.SetNewRefreshTokenAsync(username, tokenModel.RefreshToken);
            if (refreshPost == null)
            {
                return BadRequest("New refresh token didn't set to db");
            }
            
            return Ok(tokenModel);
        }

        /// <summary>
        /// Refresh tokens
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [Route("token/refresh")]
        [HttpPost]
        public async Task<IActionResult> RefreshTokenAsync([CustomizeValidator(Skip = true)][FromQuery]string token)
        {
            var refreshToken = await _tokenManager.GetRefreshToken(token);
            if (refreshToken == null)
            {
                return NotFound("There's no such token in db");
            }
            else if (refreshToken.Revoked)
            {
                return BadRequest("Refresh token has status 'Revoked'");
            }
            var person = await _tokenManager.GetPersonByNameAsync(refreshToken.Username);

            var claims = _tokenManager.GetClaimsAsync(person);
            var tokenModel = _tokenManager.GetTokenAsync(claims);

            var refreshResultToken = await _tokenManager.RefreshTokenAsync(refreshToken.Id, person, tokenModel);
            if (refreshResultToken == null)
            {
                return BadRequest("Cannot replace token in db");
            }
            return Ok(refreshResultToken);
        }
        /// <summary>
        /// Revoke token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [Route("token/revoke")]
        [HttpPost]
        public async Task<IActionResult> RevokeTokenAsync([CustomizeValidator(Skip = true)][FromQuery]string token)
        {
            var refreshToken = await _tokenManager.GetRefreshToken(token);
            if (refreshToken == null)
            {
                return NotFound("There's no such token in db");
            }
            else if (refreshToken.Revoked)
            {
                return BadRequest("Refresh token has status 'Revoked'");
            }
            
            var responseModel = await _tokenManager.RevokeRefreshTokenAsync(refreshToken);
            if (!responseModel)
            {
                return BadRequest("Refresh token cannot be replaced");
            }
            return Ok(responseModel);
        }

    }
}