using FluentValidation;
using iess_api.Models;

namespace iess_api.Validation
{
    public class GroupValidator : AbstractValidator<GroupModel>
    {
        public GroupValidator()
        {
            When(x => x != null, () => {
                RuleFor(x => x.Name).NotEmpty().WithMessage("Can't be empty")
                                    .NotNull().WithMessage("Can't be null")
                                    .MinimumLength(2).WithMessage("Must be longer than 2 symbols")
                                    .MaximumLength(32).WithMessage("Must be shorter than 32 symbols");
            });
        }
    }
}