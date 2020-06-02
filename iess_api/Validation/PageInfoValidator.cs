using System.Linq;
using FluentValidation;
using iess_api.Constants;
using iess_api.Models;
using MongoDB.Bson;

namespace iess_api.Validation
{
    public class PageInfoValidator : AbstractValidator<PageInfo>
    {
        public PageInfoValidator()
        {
            RuleFor(x => x.CurrentPage).Must(x => x > 0).WithMessage("Current page must be greater than 0.")
                                       .NotEmpty().WithMessage("Current page must be specified");
            
            RuleFor(x => x.ItemsPerPage).Must(x => x > 0).WithMessage("Number of items must be greater than 0.")
                                        .NotEmpty().WithMessage("Number of items must be specified");

            When(info => info.DateLabels != null, () =>
            {
                RuleFor(info => info.DateLabels.Count).InclusiveBetween(1, 3);
                RuleFor(info => info.DateLabels).Must(labels => labels.All(label => Types.DateRangeLabels.Contains(label)))
                    .WithMessage($"All {nameof(PageInfo.DateLabels)} must be one of the following : {Types.DateRangeLabels.ToJson()}");
            });

            When(x => !string.IsNullOrEmpty(x.OrderBy) && !string.IsNullOrWhiteSpace(x.OrderBy), () =>
                RuleFor(x => x.OrderBy).Must(x => x.Equals("ASCENDING") || x.Equals("DESCENDING"))
                                       .WithMessage("OrderBy can either be 'ASCENDING' or 'DESCENDING'"));
        }
    }
}