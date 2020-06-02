using System.Globalization;
using FluentValidation;
using iess_api.Models;

namespace iess_api.Validation
{
    public class AnswerValidator : AbstractValidator<Answer>
    {
        public AnswerValidator()
        {
            ValidatorOptions.LanguageManager.Culture = new CultureInfo("en-US");

            RuleFor(answer => answer.QuestionId).NotEmpty().MustBeObjectId(nameof(Answer.QuestionId));

        }
    }

    public class QuizAnswerValidator : AbstractValidator<QuizAnswer>
    {
        public QuizAnswerValidator()
        {
            ValidatorOptions.LanguageManager.Culture = new CultureInfo("en-US");

            RuleFor(answer => answer.IsChecked).Null();

            RuleFor(answer => answer.Assessment).Null();

        }
    }
}
