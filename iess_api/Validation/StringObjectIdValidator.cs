using FluentValidation;

namespace iess_api.Validation
{
    public class StringObjectIdValidator:AbstractValidator<string>
    {
        public StringObjectIdValidator()
        {
            RuleFor(s => s).MustBeObjectId();
        }
    }
}
