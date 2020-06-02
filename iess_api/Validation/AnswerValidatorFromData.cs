using FluentValidation;
using iess_api.Constants;
using iess_api.Models;

namespace iess_api.Validation
{
    public class PollAnswerValidatorFromData : AbstractValidator<PollAnswer>
    {
        public PollAnswerValidatorFromData(PollQuestion question)
        {
            When(answer => answer.QuestionId == question.Id, () =>
            {
                RuleFor(pollAnswer => pollAnswer.SelectedOptions).NotNull()
                    .DependentRules(() =>
                    {
                        RuleFor(pollAnswer => pollAnswer.SelectedOptions.Count)
                            .Equal(1).When(_=>Types.PollAnswerTypes[question.AnswerType]==PollAnswerType.Singleselection,ApplyConditionTo.CurrentValidator)
                            .WithMessage($"{nameof(PollAnswer.SelectedOptions)} count must be 1 for {nameof(PollAnswerType.Singleselection)} question")
                            .InclusiveBetween(1,question.AnswerOptions.Count).When(_=>Types.PollAnswerTypes[question.AnswerType]==PollAnswerType.Multiselection,ApplyConditionTo.CurrentValidator)
                            .WithMessage($"{nameof(PollAnswer.SelectedOptions)} count must be less or equal than poll question count");
                    
                        RuleForEach(pollAnswer => pollAnswer.SelectedOptions).GreaterThanOrEqualTo(0).LessThan(question.AnswerOptions.Count)
                            .WithMessage($"Each of {nameof(PollAnswer.SelectedOptions)} must be less than {nameof(question.AnswerOptions.Count)}");
                    });
            });
           
        }
    }

    public class QuizAnswerValidatorFromData : AbstractValidator<QuizAnswer>
    {
        public QuizAnswerValidatorFromData(QuizQuestion question)
        {
            When(answer => answer.QuestionId == question.Id, () =>
            {
                if (Types.QuizAnswerTypes[question.AnswerType] == QuizAnswerType.TextInput)
                {
                    RuleFor(quizAnswer => quizAnswer.SelectedOptions).Null();

                    RuleFor(quizAnswer => quizAnswer.Text).NotNull()
                        .WithMessage($"For {nameof(QuizAnswerType.TextInput)} {nameof(QuizAnswer.Text)} must be specified")
                        .Length(1, 500);
                }
                else
                {
                    RuleFor(quizAnswer => quizAnswer.Text).Null();

                    RuleFor(quizAnswer => quizAnswer.SelectedOptions).NotNull()
                        .DependentRules(() =>
                        {
                            RuleFor(quizAnswer => quizAnswer.SelectedOptions.Count)
                                .Equal(1).When(_=>Types.QuizAnswerTypes[question.AnswerType]==QuizAnswerType.Singleselection,ApplyConditionTo.CurrentValidator)
                                .WithMessage($"{nameof(QuizAnswer.SelectedOptions)} count must be 1 for {nameof(QuizAnswerType.Singleselection)} question")
                                .InclusiveBetween(1,question.AnswerOptions.Count).When(_=>Types.PollAnswerTypes[question.AnswerType]==PollAnswerType.Multiselection,ApplyConditionTo.CurrentValidator)
                                .WithMessage($"{nameof(QuizAnswer.SelectedOptions)} count must be less or equal than poll question count");

                            RuleForEach(quizAnswer => quizAnswer.SelectedOptions).GreaterThanOrEqualTo(0).LessThan(question.AnswerOptions.Count)
                                .WithMessage($"Each of {nameof(QuizAnswer.SelectedOptions)} must be less than {nameof(QuizQuestion.AnswerOptions.Count)}");
                        });
                }
            });
        }
    }
}
