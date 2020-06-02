using System.Globalization;
using System.Linq;
using FluentValidation;
using iess_api.Constants;
using iess_api.Models;

namespace iess_api.Validation
{
    public class AssessTextAnswerModelValidator:AbstractValidator<AssessTextAnswerModel>
    {
        public AssessTextAnswerModelValidator()
        {
            ValidatorOptions.LanguageManager.Culture=new CultureInfo("en-US");

            RuleFor(answerModel => answerModel.AnswerUnitId).NotEmpty().MustBeObjectId(nameof(AssessTextAnswerModel.AnswerUnitId));

            RuleFor(answerModel => answerModel.QuestionId).NotEmpty().MustBeObjectId(nameof(AssessTextAnswerModel.QuestionId));

            RuleFor(answerModel => answerModel.Assessment).NotNull();
        }
    }

    public class AssessTextAnswerModelValidatorFromData:AbstractValidator<AssessTextAnswerModel>
    {
        public AssessTextAnswerModelValidatorFromData(QuizAnswersUnit answersUnit,Quiz quiz)
        {
            ValidatorOptions.LanguageManager.Culture=new CultureInfo("en-US");

            RuleFor(answerModel => answerModel.AnswerUnitId).Must(_ => answersUnit != null)
                .WithMessage($"No such {nameof(QuizAnswersUnit)}").DependentRules(
                    () =>
                    {
                        RuleFor(answerModel => answerModel.QuestionId)
                            .Must(id => answersUnit.Answers.Any(answer => answer.QuestionId == id) &&Types.QuizAnswerTypes[quiz.Questions.Single(q=>q.Id==id).AnswerType]==QuizAnswerType.TextInput)
                            .WithMessage($"No appropriate {nameof(QuizAnswer)} was found").DependentRules(() =>
                            {
                                RuleFor(answerModel => answerModel.QuestionId)
                                    .Must((model, _) => (bool) !answersUnit.Answers.Single(a => a.QuestionId == model.QuestionId).IsChecked)
                                    .WithMessage("This question already checked");
                                RuleFor(answerModel => answerModel.Assessment)
                                    .GreaterThanOrEqualTo(0)
                                    .LessThanOrEqualTo(model=>quiz.Questions.Single(q=>q.Id==model.QuestionId).MaxAssessment)
                                    .WithMessage($"{nameof(AssessTextAnswerModel.Assessment)} must be less than or equal to {nameof(QuizQuestion.MaxAssessment)}");
                            });
                    });
        }
    }
}
