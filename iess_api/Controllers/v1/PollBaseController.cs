using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.AspNetCore;
using iess_api.Constants;
using iess_api.Interfaces;
using iess_api.Models;
using iess_api.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace iess_api.Controllers.v1
{
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class PollBaseController<TPoll,TQuestion,TAnswersUnit, TAnswer,TValidator> : ControllerBase
        where TPoll : PollBase<TQuestion>
        where TQuestion :Question
        where TAnswersUnit:AnswersUnit<TAnswer>
        where TAnswer : Answer
        where TValidator:IAnswersUnitValidatorFromData<TPoll,TQuestion,TAnswersUnit,TAnswer>, new()
    {
        private readonly IPollBaseRepository<TPoll, TAnswersUnit> _pollBaseRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IGroupManager _groupManager;
        private readonly IUserManager _userManager;
        protected readonly string SenderObjectId;
        protected readonly List<string> SenderPermissions;

        public PollBaseController(IPollBaseRepository<TPoll, TAnswersUnit> repository, ITokenManager tokenManager, IFileRepository fileRepository,
            IHttpContextAccessor accessor, IGroupManager groupManager, IUserManager userManager)
        {
            _pollBaseRepository = repository;
            _fileRepository = fileRepository;
            _groupManager = groupManager;
            _userManager = userManager;
            SenderObjectId = accessor.HttpContext.User.Claims.First(claim => claim.Type == "subject").Value;
            SenderPermissions = accessor.HttpContext.User.Claims.Where(claim=>claim.Type==ClaimsIdentity.DefaultRoleClaimType).Select(claim=>claim.Value).ToList();
        }

        /// <summary>
        /// Returns all existing instances of Poll excluding questions but including creator's name
        /// </summary>
        /// <response code="200">Returns list of Poll</response>
        /// <response code="204">No polls were found</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [Authorize(Roles = "CanViewAnyPolls")]
        [HttpGet]
        public async Task<ActionResult<PageResponse<BriefPollBaseResponse>>> GetAll([FromQuery]PageInfo info) => Ok(await _pollBaseRepository.GetAllAsync(info,SenderObjectId));

        /// <summary>
        /// Returns all existing instances of Poll request sender ever answered
        /// </summary>
        /// <response code="200">Returns list of Poll</response>
        /// <response code="204">No polls were found</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [Authorize(Roles = "CanViewAnsweredPolls")]
        [HttpGet("history")]
        public async Task<ActionResult<PageResponse<BriefPollBaseResponse>>> GetAllAnswered([FromQuery]PageInfo info) => Ok(await _pollBaseRepository.GetAllAnsweredByUser(SenderObjectId,info));

        /// <summary>
        /// Returns all existing instances of Poll available for this request sender(basing on group)
        /// </summary>
        /// <response code="200">Returns list of Poll</response>
        /// <response code="204">No polls were found</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [Authorize(Roles = "CanViewCurrentPolls")]
        [HttpGet("forme")]
        public async Task<ActionResult<IEnumerable<BriefPollBaseResponse>>> GetAllAvailable()
        {
            var user = await _userManager.GetByIdAsync(SenderObjectId);
            return Ok(user.Role == "Teacher"
                ? await _pollBaseRepository.GetAllActiveByTeacher(SenderObjectId)
                : await _pollBaseRepository.GetAllAvailableForUserAsync(SenderObjectId));
        }

        /// <summary>
        /// Returns poll instance(without correct answers) with specified <paramref name="id"/>
        /// </summary>
        /// <param name="id">Poll key of ObjectId type</param>
        /// <response code="200">Returns found Poll</response>
        /// <response code="204">No polls were found</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [Authorize(Roles = "CanViewPoll")]
        [HttpGet("{id}")]
        public async Task<ActionResult<TPoll>> Get(string id) => Ok(await _pollBaseRepository.GetByIdAsync(id));

        /// <summary>
        /// Creates new Poll instance with given data,and current time in Poll.CreationDate
        /// </summary>
        /// <remarks>
        /// Start and expire date,and eligible groups may or may not be specified
        /// Start and expire date checked to be less than current time,start date less than expire
        /// Each eligible groups checked to be between 3 and 10 characters
        /// Up-to-date list of available answer types is send in response on invalid input
        /// Must be specified at least 2 answer options with  either text between 1 and 1000 character or valid picture id or both
        /// Note that CorrectAnswerOptions is indexes of options between 0 and AnswerOptions.Count
        /// </remarks>
        /// <param name="pollBase">Data to form Poll</param>
        /// <response code="201">Returns newly created Poll</response>
        /// <response code="400">Given poll data is invalid</response>
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [Authorize(Roles = "CanCreatePoll")]
        [HttpPost]
        public async Task<ActionResult<TPoll>> Create([CustomizeValidator(RuleSet = Strings.CreateRuleSet+","+Strings.DefaultRuleSet)]TPoll pollBase)
        {
            var groups = await _groupManager.GetAllGroups();
            var result = new PollBaseValidatorFromData<TQuestion>(_fileRepository, groups.Select(groupModel=>groupModel.Name))
                .Validate(pollBase,ruleSet:Strings.DefaultRuleSet);

            result.AddToModelState(ModelState, null);
            if (!result.IsValid)
                return BadRequest(ModelState);

            pollBase.CreatorUserId=SenderObjectId;
            await _pollBaseRepository.AddAsync(pollBase);
            if(! await _fileRepository.AssociatePicturesWithPoll(pollBase,PicturesAssociateMode.Link))
                return BadRequest("Unable to associate pictures");

            return CreatedAtAction(nameof(Get), new {id = pollBase.Id}, pollBase);
        }

        /// <summary>
        /// Deletes poll instance with specified <paramref name="id"/>
        /// </summary>
        /// <param name="id">Poll key of ObjectId type</param>
        /// <response code="200">Poll was found and deleted</response>
        /// <response code="403">ObjectId of authorized user does not match Poll.CreatorUserId</response>
        /// <response code="404">Poll with specified <paramref name="id"/> wasn't found</response>
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ObjectId), 404)]
        [Authorize(Roles = "CanDeleteAnyPoll,CanDeleteOwnPoll")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var poll = await _pollBaseRepository.GetByIdAsync(id);
            if (poll==null)
                return NotFound(id);

            if (!SenderPermissions.Contains("CanDeleteAnyPoll"))
            {
                if (poll.CreatorUserId != SenderObjectId)
                    return StatusCode(StatusCodes.Status403Forbidden, "Only creator or admin can delete");
            }

            await _pollBaseRepository.DeleteAsync(id);
            return Ok();
        }

        /// <summary>
        /// Updates Poll instance with specified id with new data(except Id,CreatorUserId,CreationDate)
        /// </summary>
        /// <param name="id">Poll key of ObjectId type</param>
        /// <param name="pollBase">Data to update Poll</param>
        /// <response code="200">Poll was found and modified.</response>
        /// <response code="400">Specified <paramref name="id"/> do not correspond <paramref name="pollBase"/>.Id</response>
        /// <response code="403">ObjectId of authorized user does not match Poll.CreatorUserId</response>
        /// <response code="404">Poll with specified <paramref name="id"/> wasn't found</response>
        /// <returns>Updated entity</returns>
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(typeof(ObjectId), 404)]
        [Authorize(Roles = "CanUpdateAnyPoll,CanUpdateOwnPoll")]
        [HttpPut("{id}")]
        public async Task<ActionResult<TPoll>> Update(string id,[FromBody][CustomizeValidator(RuleSet = Strings.UpdateRuleSet+","+Strings.DefaultRuleSet)] TPoll pollBase)
        {
            if (id != pollBase.Id)
                return BadRequest("Id's must match");
            var oldPollBase= await _pollBaseRepository.GetByIdAsync(id);
            if (oldPollBase == null)
                return NotFound(id);

            if (!SenderPermissions.Contains("CanUpdateAnyPoll"))
            {
                if (oldPollBase.CreatorUserId != SenderObjectId)
                    return StatusCode(StatusCodes.Status403Forbidden, "Only creator or admin can update");
            }

            var groups = await _groupManager.GetAllGroups();
            var result = new PollBaseValidatorFromData<TQuestion>(_fileRepository, groups.Select(groupModel => groupModel.Name))
                .Validate(pollBase, ruleSet: Strings.DefaultRuleSet);
            result.AddToModelState(ModelState, null);
            if (!result.IsValid)
                return BadRequest(ModelState);

            PollBase<TQuestion>.GetReplacedPicturesList(oldPollBase,pollBase)
                .ForEach(pictureId=> _fileRepository.DeletePollFromMetadata(pictureId,id));
            await _pollBaseRepository.UpdateAsync(pollBase);
            return Ok(pollBase);
        }

        /// <summary>
        /// Sets current time as StartDate of poll with specified <paramref name="id"/>  
        /// </summary>
        /// <param name="id">Poll key of ObjectId type</param>
        /// <param name="expireDate">Updates Poll.ExpireDate</param>
        /// <response code="200">Poll was found and modified.Number of modified entries returned(should be 1)</response>
        /// <response code="400">Poll with specified <paramref name="id"/> current StartDate or passed <paramref name="expireDate"/> less than current time</response>
        /// <response code="403">ObjectId of authorized user does not match Poll.CreatorUserId</response>
        /// <response code="404">Poll with specified <paramref name="id"/> wasn't found</response>
        /// <returns>Number of modified entries (should be 1)</returns>
        [ProducesResponseType(typeof(long),200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(typeof(ObjectId), 404)]
        [Authorize(Roles = "CanStartAnyPoll,CanStartOwnPoll")]
        [HttpPatch("{id}/start")]
        public async Task<ActionResult<long>> Start(string id,[FromQuery] [Required] DateTime? expireDate)
        {
            var poll = await _pollBaseRepository.GetByIdAsync(id);
            if (poll==null)
                return NotFound(id);

            if (!SenderPermissions.Contains("CanStartAnyPoll"))
            {
                if (poll.CreatorUserId != SenderObjectId)
                    return StatusCode(StatusCodes.Status403Forbidden, "Only creator or admin can start");
            }

            if (poll.StartDate < DateTime.Now)
                return BadRequest("Poll already started");
            if (expireDate < DateTime.Now)
                return BadRequest($"Specified {nameof(expireDate)} less than current time");

            return Ok(await _pollBaseRepository.StartAsync(id,expireDate.Value));
        }

        /// <summary>
        /// Sets current time as ExpireDate of poll with specified <paramref name="id"/>  
        /// </summary>
        /// <param name="id">Poll key of ObjectId type</param>
        /// <response code="200">Poll was found and modified.Number of modified entries returned(should be 1)</response>
        /// <response code="400">Poll with specified <paramref name="id"/> current ExpireDate less than current time</response>
        /// <response code="403">ObjectId of authorized user does not match Poll.CreatorUserId</response>
        /// <response code="404">Poll with specified <paramref name="id"/> wasn't found</response>
        /// <returns>Number of modified entries (should be 1)</returns>
        [ProducesResponseType(typeof(long), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(typeof(ObjectId), 404)]
        [HttpPatch("{id}/stop")]
        [Authorize(Roles = "CanStopAnyPoll,CanStopOwnPoll")]
        public async Task<ActionResult<long>> Stop(string id)
        {
            var poll = await _pollBaseRepository.GetByIdAsync(id);
            if (poll == null)
                return NotFound(id);

            if (!SenderPermissions.Contains("CanStopAnyPoll"))
            {
                if (poll.CreatorUserId != SenderObjectId)
                    return StatusCode(StatusCodes.Status403Forbidden, "Only creator or admin can stop");
            }

            if (poll.ExpireDate < DateTime.Now)
                return BadRequest("Poll already stopped");
            return Ok(await _pollBaseRepository.StopAsync(id));
        }
        /// <summary>
        /// Get instance of AnswersUnit with specified id
        /// </summary>
        /// <param name="answerId">PollAnswer key of ObjectId type</param>
        /// <response code="200">Returns AnswersUnit</response>
        /// <response code="204">No answer was found</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [Authorize(Roles = "CanViewAnyAnswer,CanViewPublicAnswers,CanViewOwnAnswer")]
        [HttpGet("answers/{answerId}")]
        public async Task<ActionResult<TAnswersUnit>> GetAnswer(string answerId)
        {
            var answersUnit = await _pollBaseRepository.GetAnswersUnitByIdAsync(answerId);
            var pollBase = await _pollBaseRepository.GetByIdAsync(answersUnit.PollBaseId);

            if (!SenderPermissions.Contains("CanViewAnyAnswer"))
            {
                if ((bool) !pollBase.AreStatsPublic&&SenderObjectId!=answersUnit.CreatorUserId)
                    return BadRequest("Stats is private");
            }
            return Ok(answersUnit);
        }

        /// <summary>
        /// Check if current user able to answer specified Poll
        /// </summary>
        /// <param name="pollBaseId">Poll key of ObjectId type</param>
        /// <response code="200">Returns AnswersUnit</response>
        /// <response code="204">No answer was found</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ObjectId),404)]
        [Authorize(Roles = "CanViewAnyAnswer,CanViewOwnAnswer")]
        [HttpGet("{pollBaseId}/cananswer")]
        public async Task<ActionResult<bool>> CheckCurrentUserCanAnswer(string pollBaseId)
        {
            if (!await _pollBaseRepository.ExistsAsync(pollBaseId))
                return NotFound(pollBaseId);
            return Ok(await _pollBaseRepository.CheckUserCanAnswer(pollBaseId, SenderObjectId));
        }

        /// <summary>
        /// Get instance of AnswersUnit for specified Poll and current user
        /// </summary>
        /// <param name="pollBaseId">Poll key of ObjectId type</param>
        /// <response code="200">Returns AnswersUnit</response>
        /// <response code="204">No answer was found</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [Authorize(Roles = "CanViewAnyAnswer,CanViewOwnAnswer")]
        [HttpGet("{pollBaseId}/myanswer")]
        public async Task<ActionResult<TAnswersUnit>> GetAnswerForCurrentUser(string pollBaseId) => Ok(await _pollBaseRepository.GetAnswersUnitForUserAsync(pollBaseId,SenderObjectId));

        /// <summary>
        /// Returns all existing instances of PollAnswers
        /// </summary>
        /// <response code="200">Returns list of PollAnswer</response>
        /// <response code="204">No poll answers were found</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [Obsolete]
        [Authorize(Roles = "CanViewAnyAnswer")]
        [HttpGet("answers")]
        public async Task<ActionResult<IEnumerable<TAnswersUnit>>> GetAllAnswers() => Ok(await _pollBaseRepository.GetAllAnswersUnitsAsync());

        /// <summary>
        /// Returns all existing instances of PollAnswers which related with poll with specified <paramref name="id"/> questions
        /// </summary>
        /// <param name="id">Poll key of ObjectId type</param>
        /// <response code="200">Returns list of PollAnswer</response>
        /// <response code="204">No poll answers were found</response>
        /// <response code="404">Poll with specified <paramref name="id"/> wasn't found</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [Obsolete]
        [Authorize(Roles = "CanViewAnyAnswer")]
        [HttpGet("{id}/answers/full")]
        public async Task<ActionResult<IEnumerable<TAnswersUnit>>> GetPollBaseAnswers(string id)
        {
            if (!await _pollBaseRepository.ExistsAsync(id))
                return NotFound(id);
            return Ok(await _pollBaseRepository.GetPollBaseAnswersAsync(id));
        }

        /// <summary>
        /// Creates new PollAnswer instance with given data,and current time in PollAnswer.AnswerDate
        /// </summary>
        /// <remarks>
        /// Current time must be greater than related Poll.StartDate and less than ExpireDate
        /// Note that SelectedOptions is indexes of options between 0 and Poll.AnswerOptions count 
        /// For TextInput type PollAnswer.Text must be between 1 and 1000 characters, otherwise empty
        /// Valid examples:
        /// {
        ///    "creatorUserId": "5c212097856f3d2ad44d5183",
        ///    "pollQuestionId": "5c212097856f3d2ad44d5181",
        ///    "selectedOptions": null,
        ///    "text": "text"
        /// }
        /// {
        ///    "creatorUserId": "5c3379c9c7dd2f09cc734e91",
        ///    "pollQuestionId": "5c3379c9c7dd2f09cc734e92",
        ///    "selectedOptions": [
        ///         0
        ///    ]
        /// }
        /// </remarks>
        /// <param name="newAnswer">Data to form PollAnswer</param>
        /// <response code="201">Returns newly created PollAnswer</response>
        /// <response code="400">Given data is invalid,or answer of user for question already exists</response>
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [Authorize(Roles = "CanCreateAnswer")]
        [HttpPost("answers")]
        public async Task<ActionResult<TAnswersUnit>> AddAnswer(TAnswersUnit newAnswer)
        {
            newAnswer.CreatorUserId = SenderObjectId;
            var poll = await _pollBaseRepository.GetByIdAsync(newAnswer.PollBaseId);
            var oldAnswer = await _pollBaseRepository.GetUserPreviousAnswer(newAnswer.PollBaseId, SenderObjectId);

            var validator = new TValidator();
            validator.SpecifyRules(poll,oldAnswer);
            var result=validator.Validate(newAnswer);
            result.AddToModelState(ModelState,null);
            if (!result.IsValid)
                return BadRequest(ModelState);

            var userModel = await _userManager.GetByIdAsync(SenderObjectId);
            if(!poll.EligibleGroupsNames.Contains(userModel.GroupName))
                return BadRequest("Your group members are not allowed to answer this poll");

            if (oldAnswer!=null)
                await _pollBaseRepository.UpdateAnswersUnitAsync(newAnswer,oldAnswer);
            else
                await _pollBaseRepository.AddAnswersUnitAsync(newAnswer);

            return CreatedAtRoute(null,newAnswer);
        }
        /// <summary>
        /// Deletes PollAnswer instance with specified <paramref name="answerId"/>
        /// </summary>
        /// <param name="answerId">PollAnswer key of ObjectId type</param>
        /// <response code="200">PollAnswer was found and deleted</response>
        /// <response code="403">ObjectId of authorized user does not match PollAnswer.CreatorUserId</response>
        /// <response code="404">PollAnswer with specified <paramref name="answerId"/> wasn't found</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(typeof(ObjectId), 404)]
        [Authorize(Roles = "CanDeleteAnyAnswer,CanDeleteOwnAnswer")]
        [Obsolete]
        [HttpDelete("answers/{answerId}")]
        public async Task<IActionResult> DeleteAnswer(string answerId)
        {
            var answer = await _pollBaseRepository.GetAnswersUnitByIdAsync(answerId);
            if (answer==null)
                return NotFound(answerId);

            if (!SenderPermissions.Contains("CanDeleteAnyAnswer"))
            {
                if (answer.CreatorUserId != SenderObjectId)
                    return StatusCode(StatusCodes.Status403Forbidden, "Only creator or admin can delete");
            }

            await _pollBaseRepository.DeleteAnswersUnitAsync(answerId);
            return Ok();
        }
    }
}