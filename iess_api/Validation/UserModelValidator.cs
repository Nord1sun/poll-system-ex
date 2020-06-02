using FluentValidation;
using iess_api.Models;

namespace iess_api.Validation
{
    public class UserModelValidator : AbstractValidator<UserModel>
    {
        public UserModelValidator() 
        {
            When(x => !string.IsNullOrEmpty(x.Login) && !string.IsNullOrWhiteSpace(x.Login), () =>
                RuleFor(x => x.Login).Matches(@"\A\S+\z").WithMessage("Login can't contain white spaces")
                                     .MinimumLength(4).WithMessage("Login length must be greater than 4 characters.")
                                     .MaximumLength(32).WithMessage("Login length can't be greater than 32 characters."));
            
            When(x => !string.IsNullOrEmpty(x.FirstName) && !string.IsNullOrWhiteSpace(x.FirstName), () =>
                RuleFor(x => x.FirstName).Matches(@"\A\S+\z").WithMessage("FirstName can't contain white spaces")
                                         .MinimumLength(2).WithMessage("FirstName length must be greater than 2 characters.")
                                         .MaximumLength(32).WithMessage("FirstName length can't be greater than 32 characters."));
            
            When(x => !string.IsNullOrEmpty(x.LastName) && !string.IsNullOrWhiteSpace(x.LastName), () =>
                RuleFor(x => x.LastName).Matches(@"\A\S+\z").WithMessage("LastName can't contain white spaces")
                                        .MinimumLength(2).WithMessage("LastName length must be greater than 2 characters.")
                                        .MaximumLength(32).WithMessage("LastName length can't be greater than 32 characters."));
            
            When(x => !string.IsNullOrEmpty(x.PasswordHash) && !string.IsNullOrWhiteSpace(x.PasswordHash), () =>
                RuleFor(x => x.PasswordHash).Matches(@"\A\S+\z").WithMessage("PasswordHash can't contain white spaces")
                                            .MinimumLength(4).WithMessage("PasswordHash length must be greater than 4 characters.")
                                            .MaximumLength(32).WithMessage("PasswordHash length can't be greater than 32 characters."));
            
            When(x => !string.IsNullOrEmpty(x.Role) && !string.IsNullOrWhiteSpace(x.Role), () =>
                RuleFor(x => x.Role).Must(x => x.Equals("Admin") || x.Equals("Teacher") || x.Equals("Student"))
                                    .WithMessage("Role must be equal to 'Admin' or 'Teacher' or 'Student'."));
        }
    }
}