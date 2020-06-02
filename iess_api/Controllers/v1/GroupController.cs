using System.Linq;
using iess_api.Models;
using iess_api.Interfaces;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace iess_api.Controllers.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        private readonly IGroupManager _groupManager;
        
        public GroupController(IGroupManager groupManager)
        {
            _groupManager = groupManager;
        }

        /// <summary>Get All groups</summary>
        /// <response code="200">List of groups</response>
        [HttpGet]
        public async Task<IActionResult> GetAllGroups()
        {
            return Ok(await _groupManager.GetAllGroups());
        }

        /// <summary>Returns information about certain group</summary>
        /// <param name="groupId">Group's id</param>
        /// <response code="200">Group has been found.</response>
        /// <response code="400">Error! Id is equal to null.</response>
        /// <response code="404">Oops! Group hasn't been found.</response>
        [HttpGet("{groupId}")]
        public async Task<IActionResult> GetById(string groupId) 
        {
            var result = await _groupManager.GetByIdAsync(groupId);

            if (result == null)
            {
                return NotFound("Such group hasn't been found");
            }

            return Ok(result);
        }

        /// <summary>Creates a new group</summary>
        /// <param name="group">Object of GroupModel class</param>
        /// <response code="201">Group has been created.</response>
        /// <response code="400">Error! [GroupModel object is equal to null] OR [Group with such "title" already exists]</response>
        [HttpPost]
        [Authorize(Roles = "CanCreateGroup")]
        public async Task<IActionResult> Create([FromBody] GroupModel group)
        {
            if (group == null) 
            {
                return BadRequest("Object can't be equal to null.");
            }

            if (group.Name == null)
            {
                return BadRequest("Group title can't be equal to null");
            }

            var result = await _groupManager.CreateAsync(group);
            
            if (result != null)
            {
                return Ok(result);
            }
            
            return BadRequest("Such group already exists");
        }

        /// <summary>Deletes group</summary>
        /// <param name="groupId">Group's id</param>
        /// <response code="200">Group has been deleted.</response>
        /// <response code="400">Error! Id is equal to null.</response>
        /// <response code="404">Oops! Group hasn't been found.</response>
        [HttpDelete("{groupId}")]
        [Authorize(Roles = "CanDeleteGroup")]
        public async Task<IActionResult> DeleteById(string groupId)
        {
            if (await _groupManager.DeleteByIdAsync(groupId))
            {
                return Ok("Group has been successfully deleted");
            }

            return NotFound("Such group hasn't been found"); 
        }


        /// <summary>Updates group information</summary>
        /// <param name="groupId">Group id</param>
        /// <param name="group">Group information</param>
        /// <response code="200">Groups have been updated.</response>
        /// <response code="400">Error! Incorrect model has been passed.</response>
        /// <response code="404">Oops! Some group havent been found.</response>
        [HttpPut("{groupId}")]
        [Authorize(Roles = "CanUpdateGroup")]
        public async Task<IActionResult> Update(string groupId, [FromBody] GroupModel group)
        {
            if (group == null) 
            {
                return BadRequest("Object can't be equal to null.");
            }

            if (group.Name == null)
            {
                return BadRequest("Group title can't be equal to null");
            }
            if (await _groupManager.UpdateAsync(groupId, group))
            {
                return Ok("Group has been successfully updated");
            }

            return NotFound("Such group hasn't been found or already exists");        
        }
        
        /// <summary>Deletes many groups</summary>
        /// <param name="idList">List of user's id values</param>
        /// <response code="200">Groups have been deleted.</response>
        /// <response code="400">Error! Incorrect list has been passed.</response>
        /// <response code="404">Oops! Some groups havent been found.</response>

        [HttpDelete]
        [Authorize(Roles = "CanDeleteGroups")]
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

            var result = await _groupManager.DeleteManyAsync(idList);

            if (result)
            {
                return Ok("All groups have been deleted successfully");
            }

            return NotFound("Some groups haven't been found");
        }    
    }
}