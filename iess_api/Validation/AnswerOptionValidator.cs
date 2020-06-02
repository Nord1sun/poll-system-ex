using FluentValidation;
using iess_api.Interfaces;
using iess_api.Models;

namespace iess_api.Validation
{
    public class AnswerOptionValidator:AbstractValidator<AnswerOption>
    {
        public AnswerOptionValidator()
        {
            When(option => option.PictureId == null, () =>
            {
                RuleFor(option => option.QuestionText).NotNull().Length(1, 500);
            });

            When(option => option.QuestionText != null, () =>
            {
                RuleFor(option => option.QuestionText).Length(1, 500);
            });

            When(option=>option.QuestionText==null,() =>
            {
                RuleFor(option => option.PictureId).NotNull().MustBeObjectId(nameof(AnswerOption.PictureId));
            });

            When(option => option.PictureId != null, () =>
            {
                RuleFor(option => option.PictureId).MustBeObjectId(nameof(AnswerOption.PictureId));
            });
        }
    }

    public class AnswerOptionValidatorFromData : AbstractValidator<AnswerOption>
    {
        public AnswerOptionValidatorFromData(IFileRepository fileRepository)
        {
            When(option => option.PictureId != null, () =>
            {
                RuleFor(option => option.PictureId).CheckFileExists(fileRepository);
            });
        }
    }
}
