using System.Globalization;
using FluentValidation;
using iess_api.Models;

namespace iess_api.Validation
{
    public class AnswersUnitValidator<TAnswer>:AbstractValidator<AnswersUnit<TAnswer>>
    where TAnswer:Answer
    {
        public AnswersUnitValidator()
        {
            ValidatorOptions.LanguageManager.Culture = new CultureInfo("en-US");

            RuleFor(answersUnit => answersUnit.Id).Null();

            RuleFor(answersUnit => answersUnit.CreatorUserId).Null();

            RuleFor(answersUnit => answersUnit.AnswerDate).Null();

            RuleFor(answersUnit => answersUnit.PollBaseId).NotNull().MustBeObjectId(nameof(Answer.QuestionId));

            RuleForEach(answersUnit => answersUnit.Answers).SetValidator(new AnswerValidator());
        }
    }

    public class PollAnswersUnitValidator : AbstractValidator<PollAnswersUnit>
    {
        public PollAnswersUnitValidator()
        {
            Include(new AnswersUnitValidator<PollAnswer>());
        }
    }

    public class QuizAnswersUnitValidator : AbstractValidator<QuizAnswersUnit>
    {
        public QuizAnswersUnitValidator()
        {
            Include(new AnswersUnitValidator<QuizAnswer>());

            RuleFor(quizAnswersUnit => quizAnswersUnit.TotalAssessment).Null();

            RuleFor(quizAnswersUnit => quizAnswersUnit.IsChecked).Null();

            RuleFor(quizAnswersUnit => quizAnswersUnit.IsChecked).Null();

            RuleFor(quizAnswersUnit => quizAnswersUnit.CurrentReanswerCount).Null();

            RuleForEach(answersUnit => answersUnit.Answers).SetValidator(new QuizAnswerValidator());
        }
    }
}
