using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using iess_api.Interfaces;
using iess_api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver.GridFS;

namespace iess_api.Controllers.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PictureController : ControllerBase
    {
        private readonly IFileRepository _pictureRepository;
        private readonly IPictureManager _pictureManager;

        public PictureController(IFileRepository pictureRepository,IPictureManager pictureManager)
        {
            _pictureRepository = pictureRepository;
            _pictureManager = pictureManager;
        }

        /// <summary>
        /// Returns info about all saved files
        /// </summary>
        /// <response code="200">Returns list of FileInfo's</response>
        /// <response code="204">No files were saved</response>
        [Obsolete]
        [ProducesResponseType(typeof(List<GridFSFileInfo>),200)]
        [ProducesResponseType(204)]
        [Produces("application/json")]
        [HttpGet]
        public async Task<ActionResult<List<GridFSFileInfo>>> GetAllFilesInfo()
        {
            return Ok(await _pictureRepository.GetAllFiles());
        }

        /// <summary>
        /// Returns saved file
        /// </summary>
        /// <response code="200">Returns FileInfo combined with Stream of file bytes </response>
        /// <response code="404">No file with such id</response>
        [ProducesResponseType(typeof(GridFSDownloadStream),200)]
        [ProducesResponseType(404)]
        [Authorize(Roles = "CanViewPoll")]
        [HttpGet("{id}")]
        public async Task<ActionResult<GridFSDownloadStream>> Download(string id)
        {
            if (!await _pictureRepository.ExistsAsync(id))
                return NotFound(id);            
            return Ok(await _pictureRepository.DownloadAsync(id));
        }

        /// <summary>
        /// Returns saved picture fitted into 600*600 square(or unmodified if less) maintaining aspect ratio 
        /// </summary>
        /// <response code="200">Returns Stream of file bytes </response>
        /// <response code="404">No file with such id</response>
        [ProducesResponseType(typeof(Stream),200)]
        [ProducesResponseType(404)]
        [Authorize(Roles = "CanViewPoll")]
        [HttpGet("{id}/compact")]
        public async Task<ActionResult<Stream>> DownloadCompactVersion(string id)
        {
            if (!await _pictureRepository.ExistsAsync(id))
                return NotFound(id);
            var downloadStream = await _pictureRepository.DownloadAsync(id);
            return Ok(_pictureManager.GetCompactPictureVersion(downloadStream));
        }

        /// <summary>
        ///Deletes file wih specified id
        /// </summary>
        [Produces("application/json")]
        [Obsolete]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (!await _pictureRepository.ExistsAsync(id))
                return NotFound(id);
            await _pictureRepository.DeleteAsync(id);
            return Ok();
        }

        /// <summary>
        /// Saves file that has sent as form data
        /// </summary>
        /// <param name="file">Sent file</param>
        /// <response code="201">Returns objectId of saved file</response>
        /// <response code="400">No file were sent,or it's size exceeds 3MiB</response>
        [Produces("application/json")]
        [ProducesResponseType(typeof(Quiz),201)]
        [ProducesResponseType(400)]
        [Authorize(Roles = "CanCreatePoll")]
        [HttpPost]
        public async Task<ActionResult<string>> Upload([FromForm] IFormFile file)
        {
            if (file == null) return BadRequest("No file sent");
            if (file.Length > 3 * 1024 * 1024)
                BadRequest("File size must be less than 3 MiB");
            string id = await _pictureRepository.UploadAsync(file);
            return CreatedAtAction(nameof(Download),new {id},id);
        }
    }
}