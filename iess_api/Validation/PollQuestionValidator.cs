using System.Globalization;
using FluentValidation;
using iess_api.Constants;
using iess_api.Interfaces;
using iess_api.Models;
using MongoDB.Bson;

namespace iess_api.Validation
{
    public class QuestionValidator : AbstractValidator<Question>
    {
        public QuestionValidator()
        {
            ValidatorOptions.LanguageManager.Culture = new CultureInfo("en-US");

            RuleFor(question => question.QuestionText).NotEmpty().Length(1, 200);

            RuleFor(question => question.PictureId).MustBeObjectId(nameof(Question.PictureId)).When(question=>question.PictureId!=null);
        }
    }

    public class PollQuestionValidator : AbstractValidator<PollQuestion>
    {
        public PollQuestionValidator()
        {
            Include(new QuestionValidator());

            RuleFor(question => question.AnswerType).Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty()
                .Must(type => Types.PollAnswerTypes.ContainsKey(type))
                .WithMessage($"Poll answer type must be one of the following : {Types.PollAnswerTypes.Keys.ToJson()}")
                .DependentRules(() =>
                {
                    RuleFor(question => question.AnswerOptions).NotNull().DependentRules(() =>
                    {
                        RuleFor(question => question.AnswerOptions.Count).GreaterThanOrEqualTo(2);

                        RuleForEach(question => question.AnswerOptions).SetValidator(new AnswerOptionValidator());
                    });
                });
        }
    }

    public class QuizQuestionValidator : AbstractValidator<QuizQuestion>
    {
        public QuizQuestionValidator()
        {
            Include(new QuestionValidator());

            RuleFor(question => question.PreciseMatch).NotNull()
                .Unless(question =>Types.QuizAnswerTypes[question.AnswerType] == QuizAnswerType.TextInput)
                .DependentRules(() =>
            {
                RuleFor(question => question.PreciseMatch).Must(value => value==true)
                    .When(question =>Types.QuizAnswerTypes[question.AnswerType] == QuizAnswerType.Singleselection)
                    .WithMessage($"{nameof(QuizQuestion.PreciseMatch)} must be true when {nameof(QuizAnswerType.Singleselection)}");
            });

            RuleFor(question => question.MaxAssessment).NotNull().GreaterThan(0).LessThanOrEqualTo(100);

            RuleFor(question => question.AnswerType).Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty()
                .Must(type => Types.QuizAnswerTypes.ContainsKey(type))
                .WithMessage($"Poll answer type must be one of the following : {Types.QuizAnswerTypes.Keys.ToJson()}")
                .DependentRules(() =>
                {
                    RuleFor(question => question.AnswerOptions).Null()
                        .When(question =>Types.QuizAnswerTypes[question.AnswerType] == QuizAnswerType.TextInput);

                    Unless(question => Types.QuizAnswerTypes[question.AnswerType] == QuizAnswerType.TextInput,() =>
                    {
                        RuleFor(question => question.AnswerOptions).NotNull().DependentRules(() =>
                        {
                            RuleFor(question => question.AnswerOptions.Count).GreaterThanOrEqualTo(2);

                            RuleForEach(question => question.AnswerOptions).SetValidator(new AnswerOptionValidator());
                        });

                        RuleFor(question => question.CorrectAnswerOptions).NotNull().DependentRules(() =>
                        {
                            RuleFor(question => question.CorrectAnswerOptions.Count)
                                .GreaterThanOrEqualTo(1)
                                .LessThanOrEqualTo(question=>question.AnswerOptions.Count)
                                .When(question => Types.QuizAnswerTypes[question.AnswerType] == QuizAnswerType.Multiselection)
                                .Equal(1).When(question => Types.QuizAnswerTypes[question.AnswerType] == QuizAnswerType.Singleselection,
                                    ApplyConditionTo.CurrentValidator);

                            RuleForEach(question => question.CorrectAnswerOptions)
                                .GreaterThanOrEqualTo(0)
                                .LessThan(question => question.AnswerOptions.Count)
                                .WithMessage($"Each of {nameof(QuizQuestion.CorrectAnswerOptions)} must be less than poll AnswerOptions count");
                        });
                    });
                });
        }
    }

    public class QuestionValidatorFromData : AbstractValidator<Question>
    {
        public QuestionValidatorFromData(IFileRepository fileRepository)
        {
            When(question=> question.PictureId!=null,() =>
            {
                RuleFor(question => question.PictureId).CheckFileExists(fileRepository);
            });

            RuleForEach(question => question.AnswerOptions).SetValidator(new AnswerOptionValidatorFromData(fileRepository));
        }
    }
}
