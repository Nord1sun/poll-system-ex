using System;
using System.Globalization;
using System.Linq;
using FluentValidation;
using iess_api.Models;

namespace iess_api.Validation
{
    public class AnswersUnitValidatorFromData<TQuestion,TAnswer>:AbstractValidator<AnswersUnit<TAnswer>>
        where TAnswer:Answer
        where TQuestion:Question
    {
        public AnswersUnitValidatorFromData(PollBase<TQuestion> pollBase)
        {
            ValidatorOptions.LanguageManager.Culture = new CultureInfo("en-US");

            RuleFor(answersUnit => answersUnit.PollBaseId).Must(_ => pollBase!=null).WithMessage("PollBase with this Id does not exist");

            if (pollBase != null)
            {
                RuleFor(answersUnit => answersUnit.AnswerDate).Must(_ => DateTime.Now <= pollBase.ExpireDate).WithMessage("This poll is expired");

                RuleFor(answersUnit => answersUnit.AnswerDate).Must(_ => DateTime.Now > pollBase.StartDate).WithMessage("This poll is not yet started");

                RuleForEach(answersUnit => answersUnit.Answers)
                    .Must(answer => pollBase.Questions.Select(q => q.Id).Contains(answer.QuestionId))
                    .WithMessage("PollQuestion with this Id does not exist");
            }
        }
    }

    public interface IAnswersUnitValidatorFromData<TPoll,TQuestion,TAnswersUnit,TAnswer> : IValidator 
        where TPoll : PollBase<TQuestion>
        where TQuestion :Question
        where TAnswersUnit:AnswersUnit<TAnswer>
        where TAnswer : Answer
    {
        void SpecifyRules(TPoll pollBase,TAnswersUnit answersUnit);
    }

    public class PollAnswersUnitValidatorFromData:AbstractValidator<PollAnswersUnit>,IAnswersUnitValidatorFromData<Poll,PollQuestion,PollAnswersUnit,PollAnswer>
    {
        public void SpecifyRules(Poll poll,PollAnswersUnit answersUnit)
        {
            Include(new AnswersUnitValidatorFromData<PollQuestion,PollAnswer>(poll));

            if(poll==null)
                return;

            RuleFor(unit => unit.Answers.Count).GreaterThanOrEqualTo(1);

            if (answersUnit!=null)
            {
                RuleFor(u => u.PollBaseId).Must(_ => (bool) poll.IsAllowedToReanswer)
                    .WithMessage("User already has answer for this poll and reanswering is prohibited");
            }

            foreach (var question in poll.Questions)
            {
                RuleForEach(unit => unit.Answers).SetValidator(a=>new PollAnswerValidatorFromData(question));
            }
        }
    }

    public class QuizAnswersUnitValidatorFromData:AbstractValidator<QuizAnswersUnit>,IAnswersUnitValidatorFromData<Quiz,QuizQuestion,QuizAnswersUnit,QuizAnswer>
    {
        public void SpecifyRules(Quiz quiz,QuizAnswersUnit answersUnit)
        {
            Include(new AnswersUnitValidatorFromData<QuizQuestion,QuizAnswer>(quiz));

            if(quiz==null)
                return;

            RuleFor(unit => unit.Answers.Count).GreaterThanOrEqualTo(1);

            if (answersUnit!=null)
            {
                RuleFor(u => u.PollBaseId).Must(_ => (bool) quiz.IsAllowedToReanswer)
                    .WithMessage("User already has answer for this quiz and reanswering is prohibited");

                RuleFor(u => u.PollBaseId).Must(_ => answersUnit.CurrentReanswerCount<quiz.MaxReanswerCount)
                    .WithMessage("User already has answer for this quiz and reanswering limit is reached");
            }

            RuleFor(unit => unit.Answers.Count).Equal(quiz.Questions.Count);

            foreach (var question in quiz.Questions)
            {
                RuleForEach(unit => unit.Answers).SetValidator(a=>new QuizAnswerValidatorFromData(question));
            }
        }
    }
}
