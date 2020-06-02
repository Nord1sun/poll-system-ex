using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.AspNetCore;
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
    public class QuizController : PollBaseController<Quiz,QuizQuestion,QuizAnswersUnit,QuizAnswer,QuizAnswersUnitValidatorFromData>
    {
        private readonly IQuizRepository _quizRepository;

        public QuizController(IQuizRepository quizRepository, ITokenManager tokenManager, IFileRepository fileRepository, IHttpContextAccessor accessor, IGroupManager groupManager,IUserManager userManager) 
            : base(quizRepository, tokenManager, fileRepository, accessor, groupManager,userManager)
        {
            _quizRepository = quizRepository;
        }

        /// <summary>
        /// Returns only correct answers options of poll instance with specified <paramref name="quizId"/>
        /// </summary>
        /// <param name="quizId">Poll key of ObjectId type</param>
        /// <response code="200">Returns found Poll</response>
        /// <response code="204">No polls were found</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [Authorize(Roles = "CanViewAnyQuizCorrectOptions")]
        [HttpGet("{quizId}/correct")]
        public async Task<ActionResult<Quiz>> GetQuizCorrectAnswerOptions(string quizId) => Ok(await _quizRepository.GetQuizCorrectAnswerOptionsAsync(quizId));

        /// <summary>
        /// Returns all shallow copy of specified quiz AnswersUnits with creators info grouped by group name
        /// </summary>
        /// /// <param name="quizId">Question key of ObjectId type</param>
        /// <response code="200">Returns list groups by GroupName of PollAnswer with creator info</response>
        /// <response code="204">No quiz or answers were found</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [Authorize(Roles = "CanViewAnyAnswer,CanViewPublicAnswers")]
        [HttpGet("{quizId}/answers")]
        public async Task<ActionResult<IEnumerable<IGrouping<string, BriefQuizAnswerResponse>>>> GetQuizAnswers(string quizId)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId);

            if (!SenderPermissions.Contains("CanViewAnyAnswer"))
            {
                if ((bool) !quiz.AreStatsPublic&&SenderObjectId!=quiz.CreatorUserId)
                    return BadRequest("Stats is private");
            }

            return Ok(await _quizRepository.GetQuizAnswersAsync(quizId));
        }

        /// <summary>
        /// Sets correctness of specified Answer in specified AnswerUnit(updates Answer.IsCorrect,Answer.IsChecked,AnswerUnit.IsChecked,AnswerUnit.TotalAssessment)
        /// </summary>
        /// <response code="200">AnswerUnit was found and modified</response>
        /// <response code="403">ObjectId of authorized user does not match Quiz.CreatorUserId</response>
        /// <returns>Number of modified entries (should be 1)</returns>
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [Authorize(Roles = "CanAssessAnyAnswer,CanAssessOwnQuizAnswers")]
        [HttpPatch("answers")]
        public async Task<ActionResult<long>> AssessTextAnswer(AssessTextAnswerModel assessModel)
        {
            var answersUnit = await _quizRepository.GetAnswersUnitByIdAsync(assessModel.AnswerUnitId);
            var quiz = await _quizRepository.GetByIdAsync(answersUnit.PollBaseId);

            if (!SenderPermissions.Contains("CanAssessAnyAnswer"))
            {
                if (quiz.CreatorUserId != SenderObjectId)
                    return StatusCode(StatusCodes.Status403Forbidden, "Only creator or admin can assess answers");
            }

            var result= new AssessTextAnswerModelValidatorFromData(answersUnit,quiz).Validate(assessModel);
            result.AddToModelState(ModelState, null);
            if (!result.IsValid)
                return BadRequest(ModelState);
            
            return Ok(await _quizRepository.AssessTextAnswerAsync(assessModel));
        }
    }
}