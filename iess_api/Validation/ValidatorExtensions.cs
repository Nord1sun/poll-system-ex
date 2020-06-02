using FluentValidation;
using iess_api.Interfaces;
using MongoDB.Bson;

namespace iess_api.Validation
{
    public static class ValidatorExtensions
    {
        public static IRuleBuilderOptions<T, string> MustBeObjectId<T>(this IRuleBuilder<T, string> rule,string propertyName="Id") 
        {
            return rule.Must(studentUserId => ObjectId.TryParse(studentUserId, out _))
                .WithMessage($"Specified {propertyName} cannot be converted to ObjectId");
        }

        public static IRuleBuilderOptions<T, string> CheckFileExists<T>(this IRuleBuilder<T, string> rule,IFileRepository fileRepository) 
        {
            return rule.Must(id => fileRepository.ExistsAsync(id).Result).WithMessage("No such file");
        }
    }
}
