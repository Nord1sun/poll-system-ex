using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FluentValidation;
using iess_api.Constants;
using iess_api.Interfaces;
using iess_api.Models;

namespace iess_api.Validation
{
    public class PollBaseValidator<TQuestion>:AbstractValidator<PollBase<TQuestion>> where TQuestion:Question
    {
        public PollBaseValidator()
        {
            ValidatorOptions.LanguageManager.Culture=new CultureInfo("en-US");

            RuleSet(Strings.CreateRuleSet,() =>
            {
                RuleFor(pollBase => pollBase.Id).Null();

                RuleFor(pollBase => pollBase.CreatorUserId).Null();

                RuleFor(pollBase => pollBase.CreationDate).Null();

                CommonRules();
            });

            RuleSet(Strings.UpdateRuleSet, () =>
            {
                RuleFor(pollBase => pollBase.Id).NotEmpty().MustBeObjectId(nameof(PollBase<TQuestion>.Id));

                CommonRules();
            });
        }

        private void CommonRules()
        {
            RuleFor(pollBase => pollBase.Title).NotEmpty().Length(1, 100);

            RuleFor(pollBase => pollBase.StartDate)
                .GreaterThan(DateTime.Now).When(pollBase => pollBase.StartDate != null)
                .LessThan(pollBase => pollBase.ExpireDate).When(pollBase => pollBase.ExpireDate != null);

            RuleFor(pollBase => pollBase.ExpireDate).GreaterThan(DateTime.Now).When(pollBase => pollBase.ExpireDate != null);

            RuleForEach(pollBase => pollBase.EligibleGroupsNames).NotEmpty().Length(3,10);

            RuleFor(pollBase => pollBase.Questions).NotNull().DependentRules(() =>
            {
                RuleFor(pollBase => pollBase.Questions.Count).GreaterThanOrEqualTo(1);
            });

            RuleFor(pollBase => pollBase.IsAllowedToReanswer).NotNull();
        }
    }

    public class PollValidator : AbstractValidator<Poll>
    {
        public PollValidator()
        {
            Include(new PollBaseValidator<PollQuestion>());

            //RuleFor(poll => poll.AreAnswersAnonymous).NotNull();

            RuleForEach(poll => poll.Questions).SetValidator(new PollQuestionValidator());
        }
    }

    public class QuizValidator : AbstractValidator<Quiz>
    {
        public QuizValidator()
        {
            Include(new PollBaseValidator<QuizQuestion>());

            RuleSet(Strings.CreateRuleSet, () =>
            {
                RuleFor(quiz => quiz.MaxAssessment).Null();

            });

            RuleFor(pollBase => pollBase.AreStatsPublic).NotNull();

            //RuleFor(quiz => quiz.MaxReanswerCount).NotNull().InclusiveBetween(0,50);

            RuleForEach(quiz => quiz.Questions).SetValidator(new QuizQuestionValidator());
        }
    }

    public class PollBaseValidatorFromData<TQuestion> : AbstractValidator<PollBase<TQuestion>> where TQuestion:Question
    {
        public PollBaseValidatorFromData(IFileRepository fileRepository, IEnumerable<string> groupsNamesList)
        {
            ValidatorOptions.LanguageManager.Culture = new CultureInfo("en-US");

            RuleForEach(pollBase => pollBase.EligibleGroupsNames).Must(groupsNamesList.Contains).When(pollBase => pollBase.EligibleGroupsNames != null).WithMessage("No such group");

            RuleForEach(pollBase => pollBase.Questions).SetValidator(new QuestionValidatorFromData(fileRepository));
        }
    }
}
