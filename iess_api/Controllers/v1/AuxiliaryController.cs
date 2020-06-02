using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.AspNetCore;
using iess_api.Constants;
using iess_api.Interfaces;
using iess_api.Models;
using iess_api.Repositories;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace iess_api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuxiliaryController : ControllerBase
    {
        private readonly IPollRepository _pollRepository;
        private readonly IQuizRepository _quizRepository;
        private readonly IUserManager _userManager;
        private readonly IGroupRepository _groupRepository;
        private readonly IUserRepository _userRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IGroupManager _groupManager;
        List<UserModel> _userModels;
        List<GroupModel> _groupModels;

        public AuxiliaryController(IPollRepository pollRepository,IQuizRepository quizRepository, IGroupManager groupManager,
            IUserManager userManager, IGroupRepository groupRepository, IFileRepository fileRepository, IUserRepository userRepository)
        {
            _pollRepository = pollRepository;
            _quizRepository = quizRepository;
            _userManager = userManager;
            _groupRepository = groupRepository;
            _fileRepository = fileRepository;
            _groupManager = groupManager;
            _userRepository = userRepository;
        }

        [Route("/status")]
        [HttpGet]
        public IActionResult Status()
        {
            return Ok("ok!");
        }

        /// <summary>
        /// Seed db with few users, groups and polls with answers
        /// </summary>
        [HttpPost("seed")]
        public async Task<IActionResult> SeedDatabase()
        {
            SeedGroups();
            await SeedUsers();
            await SeedPollsAndPollAnswers();
            await SeedQuizzesAndQuizAnswers();
            return Ok("databaseSeeded");
        }

        /// <summary>
        /// Changes group names to ids
        /// </summary>
        [HttpPost("/group/change")]
        public async Task<IActionResult> ChangeGroup() 
        {
            var users = await _userRepository.GetAllAsync();
            users.ForEach(async user => {
                user.GroupName = await _groupManager.GetIdByName(user.GroupName);
                await _userManager.UpdateUserAsync(user.UserId, user);
            });
            
            return Ok("Group names changed to group ids");
        }

        /// <summary>
        /// Clear database collection
        /// </summary>
        /// <param name="collectionName"> Must consider case e.g. Poll,Quiz,PollAnswersUnit,QuizAnswersUnit,Users,Groups,Pictures or All</param>
        [ProducesResponseType(200)]
        [HttpDelete("drop/{collectionName}")]
        public ActionResult Drop([CustomizeValidator(Skip = true)]string collectionName)
        {
            if (collectionName == "All")
            {
                "Poll,Quiz,PollAnswersUnit,QuizAnswersUnit,Users,Groups".Split(',').ToList().ForEach(name=>_pollRepository.PollBaseCollection.Database.DropCollection(name));
            }
            else if (collectionName == "Pictures")
            {
                _fileRepository.Bucket.Drop();
            }
            else
            {
                _pollRepository.PollBaseCollection.Database.DropCollection(collectionName);
            }
            return Ok();
        }
        #region seed functions=
        private async void SeedGroups()
        {
            _groupModels = new List<GroupModel>
            {
                new GroupModel { Name = "201" }, new GroupModel { Name = "301" }, new GroupModel { Name = "401" }, new GroupModel { Name = "501" }, new GroupModel { Name = "671" },
                new GroupModel { Name = "101" }, new GroupModel { Name = "202" }, new GroupModel { Name = "302" }, new GroupModel { Name = "402" }, new GroupModel { Name = "505" }, new GroupModel { Name = "691" },
                new GroupModel { Name = "102" }, new GroupModel { Name = "205" }, new GroupModel { Name = "303" }, new GroupModel { Name = "403" }, new GroupModel { Name = "507" }, new GroupModel { Name = "181" },
                new GroupModel { Name = "103" }, new GroupModel { Name = "208" }, new GroupModel { Name = "304" }, new GroupModel { Name = "405" }, new GroupModel { Name = "511" }, new GroupModel { Name = "183" },
                new GroupModel { Name = "105" }, new GroupModel { Name = "209" }, new GroupModel { Name = "305" }, new GroupModel { Name = "406" }, new GroupModel { Name = "517" }, new GroupModel { Name = "184" },
                new GroupModel { Name = "108" }, new GroupModel { Name = "211-п" }, new GroupModel { Name = "308" }, new GroupModel { Name = "413" }, new GroupModel { Name = "515" }, new GroupModel { Name = "281" },
                new GroupModel { Name = "109" }, new GroupModel { Name = "211-ф" }, new GroupModel { Name = "313" }, new GroupModel { Name = "415" }, new GroupModel { Name = "516" }, new GroupModel { Name = "283" },
                new GroupModel { Name = "111" }, new GroupModel { Name = "213" }, new GroupModel { Name = "315" }, new GroupModel { Name = "417" }, new GroupModel { Name = "518" }, new GroupModel { Name = "284" },
                new GroupModel { Name = "115" }, new GroupModel { Name = "217" }, new GroupModel { Name = "335" }, new GroupModel { Name = "435" }, new GroupModel { Name = "521" }, new GroupModel { Name = "285" },
                new GroupModel { Name = "135" }, new GroupModel { Name = "235" }, new GroupModel { Name = "317" }, new GroupModel { Name = "418" }, new GroupModel { Name = "531" }, new GroupModel { Name = "381" },
                new GroupModel { Name = "118" }, new GroupModel { Name = "218" }, new GroupModel { Name = "318" }, new GroupModel { Name = "421" }, new GroupModel { Name = "534" }, new GroupModel { Name = "383" },
                new GroupModel { Name = "121" }, new GroupModel { Name = "221" }, new GroupModel { Name = "321" }, new GroupModel { Name = "431" }, new GroupModel { Name = "541" }, new GroupModel { Name = "384" },
                new GroupModel { Name = "134" }, new GroupModel { Name = "231" }, new GroupModel { Name = "331" }, new GroupModel { Name = "434" }, new GroupModel { Name = "545" }, new GroupModel { Name = "385" },
                new GroupModel { Name = "140" }, new GroupModel { Name = "234" }, new GroupModel { Name = "334" }, new GroupModel { Name = "441" }, new GroupModel { Name = "546" }, new GroupModel { Name = "481" },
                new GroupModel { Name = "141" }, new GroupModel { Name = "241" }, new GroupModel { Name = "341" }, new GroupModel { Name = "443" }, new GroupModel { Name = "551а" }, new GroupModel { Name = "483" },
                new GroupModel { Name = "142" }, new GroupModel { Name = "242" }, new GroupModel { Name = "342" }, new GroupModel { Name = "444" }, new GroupModel { Name = "551б" }, new GroupModel { Name = "484" },
                new GroupModel { Name = "144" }, new GroupModel { Name = "243" }, new GroupModel { Name = "343" }, new GroupModel { Name = "445" }, new GroupModel { Name = "561" }, new GroupModel { Name = "581" },
                new GroupModel { Name = "143" }, new GroupModel { Name = "244/1" }, new GroupModel { Name = "344/1" }, new GroupModel { Name = "446" }, new GroupModel { Name = "563" }, new GroupModel { Name = "582" },
                new GroupModel { Name = "146" }, new GroupModel { Name = "244/2" }, new GroupModel { Name = "344/2" }, new GroupModel { Name = "447" }, new GroupModel { Name = "565" }, new GroupModel { Name = "583" },
                new GroupModel { Name = "148" }, new GroupModel { Name = "245" }, new GroupModel { Name = "345" }, new GroupModel { Name = "448" }, new GroupModel { Name = "571" }, new GroupModel { Name = "584" },
                new GroupModel { Name = "151" }, new GroupModel { Name = "246" }, new GroupModel { Name = "346" }, new GroupModel { Name = "451" }, new GroupModel { Name = "591" }, new GroupModel { Name = "2201" },
                new GroupModel { Name = "152" }, new GroupModel { Name = "248" }, new GroupModel { Name = "348" }, new GroupModel { Name = "452" }, new GroupModel { Name = "525" }, new GroupModel { Name = "2202" },
                new GroupModel { Name = "153" }, new GroupModel { Name = "251" }, new GroupModel { Name = "351" }, new GroupModel { Name = "461" },                                  new GroupModel { Name = "2203" },
                new GroupModel { Name = "131" }, new GroupModel { Name = "252" }, new GroupModel { Name = "352" }, new GroupModel { Name = "463" },                                  new GroupModel { Name = "2204" },
                new GroupModel { Name = "163" }, new GroupModel { Name = "261" }, new GroupModel { Name = "361" }, new GroupModel { Name = "465" },                                  new GroupModel { Name = "2205" },
                new GroupModel { Name = "165" }, new GroupModel { Name = "263" }, new GroupModel { Name = "363" }, new GroupModel { Name = "466" },                                  new GroupModel { Name = "2206" },
                new GroupModel { Name = "166" }, new GroupModel { Name = "265" }, new GroupModel { Name = "365" }, new GroupModel { Name = "471" },                                  new GroupModel { Name = "2207" },
                new GroupModel { Name = "171" }, new GroupModel { Name = "271" }, new GroupModel { Name = "371" }, new GroupModel { Name = "491" },
                new GroupModel { Name = "191" }, new GroupModel { Name = "291" }, new GroupModel { Name = "391" }, new GroupModel { Name = "494" },
                new GroupModel { Name = "192" }, new GroupModel { Name = "292" }, new GroupModel { Name = "392" },
                new GroupModel { Name = "293" }
            };

            foreach (var groupModel in _groupModels)
            {
                await _groupRepository.PostAsync(groupModel);
            }
        }

        private async Task SeedPollsAndPollAnswers()
        {

            var id1 = ObjectId.GenerateNewId().ToString();
            var id2 = ObjectId.GenerateNewId().ToString();
            var polls = new List<Poll>
            {
                new Poll
                {
                    CreatorUserId = _userModels[1].UserId,
                    StartDate = DateTime.Now,
                    ExpireDate = DateTime.MaxValue,
                    Title = "title",
                    EligibleGroupsNames = _groupModels.Select(g=>g.Name).ToList(),
                    AreStatsPublic = false,
                    IsAllowedToReanswer = true,
                    Questions = new List<PollQuestion>
                    {
                        new PollQuestion
                        {
                            Id = id1,
                            QuestionText = "?",
                            AnswerOptions = new List<AnswerOption>
                            {
                                new AnswerOption {QuestionText = "1"},
                                new AnswerOption {QuestionText = "2"},
                                new AnswerOption {QuestionText = "3"},
                            },
                            AnswerType = Types.GetPollAnswerTypeAsString(PollAnswerType.Multiselection),
                        },
                        new PollQuestion
                        {
                            Id = id2,
                            QuestionText = "?",
                            AnswerOptions = new List<AnswerOption>
                            {
                                new AnswerOption {QuestionText = "1"},
                                new AnswerOption {QuestionText = "2",}
                            },
                            AnswerType = Types.GetPollAnswerTypeAsString(PollAnswerType.Singleselection),
                        },
                    }
                },
            };
            foreach (var poll in polls)
            {
                await _pollRepository.AddAsync(poll);
            }
            var pollAnswersUnits = new List<PollAnswersUnit>
            {
                new PollAnswersUnit()
                {
                    CreatorUserId = _userModels[2].UserId,
                    PollBaseId = polls[0].Id,
                    AnswerDate = DateTime.Now,
                    Answers = new List<PollAnswer>()
                    {
                        new PollAnswer
                        {
                            QuestionId = id1,
                            SelectedOptions = new List<int>{0,1},
                        },
                        new PollAnswer
                        {
                            QuestionId = id2,
                            SelectedOptions = new List<int>{0},
                        }
                    }
                },
                new PollAnswersUnit()
                {
                    CreatorUserId = _userModels[0].UserId,
                    PollBaseId = polls[0].Id,
                    AnswerDate = DateTime.Now,
                    Answers = new List<PollAnswer>()
                    {
                        new PollAnswer
                        {
                            QuestionId = id1,
                            SelectedOptions = new List<int>{0,2},
                        },
                        new PollAnswer
                        {
                            QuestionId = id2,
                            SelectedOptions = new List<int>{1},
                        }
                    }
                }
            };
            foreach (var pollAnswersUnit in pollAnswersUnits)
            {
                await _pollRepository.AddAnswersUnitAsync(pollAnswersUnit);
            }
        }

        private async Task SeedQuizzesAndQuizAnswers()
        {

            var id1 = ObjectId.GenerateNewId().ToString();
            var id2 = ObjectId.GenerateNewId().ToString();
            var id3 = ObjectId.GenerateNewId().ToString();
            var quizzes = new List<Quiz>
            {
                new Quiz
                {
                    CreatorUserId = _userModels[1].UserId,
                    StartDate = DateTime.Now,
                    ExpireDate = DateTime.MaxValue,
                    Title = "title",
                    EligibleGroupsNames = _groupModels.Select(g=>g.Name).ToList(),
                    AreStatsPublic = true,
                    MaxAssessment = 3,
                    Questions = new List<QuizQuestion>
                    {
                        new QuizQuestion
                        {
                            Id = id1,
                            QuestionText = "?",
                            AnswerOptions = new List<AnswerOption>
                            {
                                new AnswerOption {QuestionText = "1"},
                                new AnswerOption {QuestionText = "2"},
                                new AnswerOption {QuestionText = "3"}
                            },
                            CorrectAnswerOptions = new List<int> {0, 1},
                            AnswerType = Types.GetQuizAnswerTypeAsString(QuizAnswerType.Multiselection),
                            MaxAssessment = 1,
                            PreciseMatch = false
                        },
                        new QuizQuestion
                        {
                            Id = id2,
                            QuestionText = "?",
                            AnswerOptions = new List<AnswerOption>
                            {
                                new AnswerOption {QuestionText = "1"},
                                new AnswerOption {QuestionText = "2"}
                            },
                            CorrectAnswerOptions = new List<int> {0},
                            AnswerType = Types.GetQuizAnswerTypeAsString(QuizAnswerType.Singleselection),
                            MaxAssessment = 1,
                            PreciseMatch = true
                        },
                        new QuizQuestion
                        {
                            Id = id3,
                            QuestionText = "?",
                            AnswerType = Types.GetQuizAnswerTypeAsString(QuizAnswerType.TextInput),
                            MaxAssessment = 1,
                            PreciseMatch = null
                        },
                    }
                },
            };
            foreach (var quiz in quizzes)
            {
                await _quizRepository.AddAsync(quiz);
            }
            var quizAnswersUnits = new List<QuizAnswersUnit>
            {
                new QuizAnswersUnit
                {
                    CreatorUserId = _userModels[2].UserId,
                    PollBaseId = quizzes[0].Id,
                    AnswerDate = DateTime.Now,
                    Answers = new List<QuizAnswer>
                    {
                        new QuizAnswer
                        {
                            QuestionId = id1,
                            SelectedOptions = new List<int>{0,1},
                        },
                        new QuizAnswer
                        {
                            QuestionId = id2,
                            SelectedOptions = new List<int>{0},
                        },
                        new QuizAnswer
                        {
                            QuestionId = id3,
                            Text = "text"
                        }
                    }
                }
               
            };
            foreach (var quizAnswersUnit in quizAnswersUnits)
            {
                await _quizRepository.AddAnswersUnitAsync(quizAnswersUnit);
            }
        }

        private async Task SeedUsers()
        {
            
            _userModels = new List<UserModel>
            {
                new UserModel {Login = "admin", PasswordHash = "admin", Role = "Admin",FirstName = "John",LastName = "Mongo",GroupName ="501"},
                new UserModel {Login = "teacher", PasswordHash = "teacher", Role = "Teacher",FirstName = "Dongo",LastName = "Kongo",GroupName ="501"},
                new UserModel {Login = "user", PasswordHash = "user", Role = "Student",FirstName = "Longo",LastName = "Wongo",GroupName ="501"}
            };


            foreach (var userModel in _userModels)
            {
                await _userManager.CreateUserAsync(userModel);
            }

        }
        #endregion
    }
}
