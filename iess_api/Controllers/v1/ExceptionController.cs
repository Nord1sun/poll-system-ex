using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using iess_api.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace iess_api.Controllers.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ExceptionController : ControllerBase
    {
        private readonly IExceptionManager _exceptionManager;
        public ExceptionController(IExceptionManager exceptionManager)
        {
            _exceptionManager = exceptionManager;
        }

        /// <summary>
        /// Get all exception from db.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetAllExceptions()
        {
            var exceptions = await _exceptionManager.GetAllExceptions();
            if (exceptions == null)
            {
                return NotFound();
            }
            return Ok(exceptions);
        }
    }
}