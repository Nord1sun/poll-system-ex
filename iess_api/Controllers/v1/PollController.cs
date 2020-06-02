using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using iess_api.Interfaces;
using iess_api.Models;
using iess_api.Repositories;
using iess_api.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace iess_api.Controllers.v1
{
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class PollController : PollBaseController<Poll,PollQuestion,PollAnswersUnit, PollAnswer,PollAnswersUnitValidatorFromData>
    {
        private readonly IPollRepository _pollRepository;

        public PollController(IPollRepository pollRepository, ITokenManager tokenManager, IFileRepository fileRepository, IHttpContextAccessor accessor, IGroupManager groupManager, IUserManager userManager) 
            : base(pollRepository, tokenManager, fileRepository, accessor, groupManager,userManager)
        {
            _pollRepository = pollRepository;
        }

        
        /// <summary>
        /// Returns all instances of specified question PollAnswer's with creators info grouped by group name
        /// </summary>
        /// /// <param name="questionId">Question key of ObjectId type</param>
        /// <response code="200">Returns list groups by GroupName of PollAnswer with creator info</response>
        /// <response code="204">No answers were found</response>
        [Obsolete]
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [Authorize(Roles = "CanViewAnyAnswer,CanViewPublicAnswers")]
        [HttpGet("question/{questionId}/answers")]
        public async Task<ActionResult<List<IGrouping<string, PollRepository.PollAnswerResponse>>>> GetPollQuestionAnswers(string questionId)
        {
            var poll = await _pollRepository.GetPollBaseByQuestionIdAsync(questionId);

            if (!SenderPermissions.Contains("CanViewAnyAnswer"))
            {
                if ((bool) !poll.AreStatsPublic&&SenderObjectId!=poll.CreatorUserId)
                    return BadRequest("Stats is private");
            }
            if ((bool) poll.AreAnswersAnonymous)
                return BadRequest("Answers are anonymous");

            return Ok(await _pollRepository.GetPollQuestionAnswersAsync(questionId));
        }

        /// <summary>
        /// Returns count of answers for each option of specified question
        /// </summary>
        /// /// <param name="questionId">Question key of ObjectId type</param>
        /// <response code="200">Returns dictionary of option index as key and count of answers with this options as value</response>
        [ProducesResponseType(200)]
        [Authorize(Roles = "CanViewAnyPollStats,CanViewPublicPollsStats")]
        [HttpGet("question/{questionId}/plot")]
        public async Task<ActionResult<Dictionary<int, int>>> GetPollQuestionAnswersPlotInfo(string questionId)
        {
            var poll = await _pollRepository.GetPollBaseByQuestionIdAsync(questionId);

            if (!SenderPermissions.Contains("CanViewAnyPollStats"))
            {
                if ((bool) !poll.AreStatsPublic&&SenderObjectId!=poll.CreatorUserId)
                    return BadRequest("Stats is private");
            }

            return Ok(await _pollRepository.GetPollQuestionAnswersPlotInfoAsync(questionId));
        }
    }
}
