using System.Linq;
using iess_api.Models;
using iess_api.Interfaces;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace iess_api.Controllers
{
    
    [ApiController]
    [Produces("application/json")]
    [Route("api/v1/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserManager _manager;
        private readonly ITokenManager _tokenManager;

        public UsersController(IUserManager manager, ITokenManager tokenManager) 
        {
            _manager = manager;
            _tokenManager = tokenManager;
        } 

        /// <summary>Returns a collection of users</summary>
        /// <response code="200">List of all users.</response>
        [HttpGet]
        [Authorize(Roles = "CanViewAllUsers")]
        public async Task<ActionResult<PageResponse<UserModel>>> GetAllUsers([FromQuery] PageInfo info)
        {
            return Ok(await _manager.GetAllAsync(info));
        }

        /// <summary>Returns information about certain user</summary>
        /// <param name="id">User's id</param>
        /// <response code="200">User has been found.</response>
        /// <response code="400">Error! Id is equal to null.</response>
        /// <response code="404">Oops! User hasn't been found.</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserModel>> GetUserInfo(string id)
        {
            if (id == null)
            {
                return BadRequest("Id is equal to null.");
            }

            var result = await _manager.GetByIdAsync(id);

            if (result == null)
            {
                return NotFound("User with such id hasn't been found.");
            }

            return Ok(result);
        }

        /// <summary>Creates a new user</summary>
        /// <param name="model">Object of UserModel class</param>
        /// <response code="201">User has been created.</response>
        /// <response code="400">Error! [UserModel object is equal to null] OR [User with such “login” already exists]</response>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserModel model)
        {
            if (model == null)
            {
                return BadRequest("Incorrect model has been recivied.");
            }

            var result = await _manager.CreateUserAsync(model);

            if (result == null)
            {
                return BadRequest("User with such login already exists.");
            }

            return Created("", result);
        }

        /// <summary>Deletes user</summary>
        /// <param name="id">User's id</param>
        /// <response code="200">User has been deleted.</response>
        /// <response code="400">Error! Id is equal to null.</response>
        /// <response code="404">Oops! User hasn't been found.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "CanDeleteUser")]
        public async Task<ActionResult<bool>> DeleteUser(string id)
        {
            if (id == null)
            {
                return BadRequest("Id is equal to null.");
            }

            var result = await _manager.DeleteUserAsync(id);

            if (result)
            {
                return Ok("User with such id has been successfully deleted.");
            }
            else
            {
                return NotFound("User with such id hasn't been found.");
            }
        }
        
        /// <summary>Deletes many users</summary>
        /// <param name="idList">List of user's id values</param>
        /// <response code="200">Users have been deleted.</response>
        /// <response code="400">Error! Incorrect list has been passed.</response>
        /// <response code="404">Oops! Some user's havent been found.</response>
        [HttpDelete]
        [Authorize(Roles = "CanDeleteUsers")]
        public async Task<ActionResult<bool>> DeleteMany([FromBody]string[] idList) 
        {
            if (idList == null) 
            {
                return BadRequest("Array is equal to null");
            }

            if (idList.Length == 0) 
            {
                return BadRequest("Array is empty");
            }

            if (idList.Any(x => string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x))) 
            {
                return BadRequest("Some id values are empty strings");
            }

            var result = await _manager.DeleteManyUsersAsync(idList);

            if (result)
            {
                return Ok("All users have been deleted successfully");
            }

            return NotFound("Some users haven't been found");
        }
        
        /// <summary>Updates user's information</summary>
        /// <param name="id">User's id</param>
        /// <param name="model">Object of UserModel class</param>
        /// <response code="200">User's information has been updated.</response>
        /// <response code="400">Error! [UserModel object is equal to null] OR [Id is equal to null]</response>
        /// <response code="404">Oops! User hasn't been found.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = "CanUpdateUser, CanUpdateOwnInformation")]
        public async Task<ActionResult<bool>> UpdateUser(string id, [FromBody] UserModel model)
        {
            var currentUserId = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "subject").Value;
            var currentUser = await _manager.GetByIdAsync(currentUserId);
            var currentUserRole = currentUser.Role;
            var refreshTokenModel = await _tokenManager.GetRefreshTokenByUsername(currentUser.Login);
            
            if (id == null) 
            {
                return BadRequest("Id is equal to null.");
            }

            if (model == null)
            {
                return BadRequest("Incorrect model has been recivied.");
            }
            
            if (refreshTokenModel == null)
            {
                return BadRequest("Something went wrong in database");
            }

            if (model.Login != refreshTokenModel.Username)
            {
                var newRefreshTokenModel = await _tokenManager.SetNewRefreshTokenAsync(model.Login, refreshTokenModel.Token);
            }

            if (currentUserRole == "Admin" || (currentUserRole != "Admin" && currentUserId == id)) 
            {
                if (await _manager.UpdateUserAsync(id, model))
                {
                    return Ok("Information about user with such id has been successfully updated.");
                }

                return NotFound("User with such id hasn't been found.");
            }
            
            return BadRequest("You are not allowed to do this.");
        }


        /// <summary>Updates users' information</summary>
        /// <param name="users">Object of UserModel class</param>
        /// <response code="200">Information has been updated.</response>
        /// <response code="400">Error! [Array is equal to null] or [Some value is equal to null].</response>
        /// <response code="404">Oops! Some user hasn't been found.</response>
        [HttpPut]
        [Authorize(Roles = "CanUpdateUsers")]
        public async Task<ActionResult<bool>> UpdateMany([FromBody]UserModel[] users)
        {

            if (users == null) 
            {
                return BadRequest("Array is equal to null");
            }

            if (users.Length == 0) 
            {
                return BadRequest("Array is empty");
            }

            if (users.Any(x => x == null)) 
            {
                return BadRequest("Some values are equal to null");
            }

            var result = await _manager.UpdateManyUsersAsync(users);

            if (result) 
            {
                return Ok("Users have been successfully updated");
            }

            return NotFound("Some users haven't been found");
        }
    }
}
