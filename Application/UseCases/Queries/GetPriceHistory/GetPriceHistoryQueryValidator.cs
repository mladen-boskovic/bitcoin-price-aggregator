using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.UseCases.Queries.GetPriceHistory
{
    public class GetPriceHistoryQueryValidator : AbstractValidator<GetPriceHistoryQuery>
    {
        public GetPriceHistoryQueryValidator()
        {
            RuleFor(x => x.From).NotEqual(default(DateTime)).WithMessage("'from' is required.");
            RuleFor(x => x.To).NotEqual(default(DateTime)).WithMessage("'to' is required.");
            RuleFor(x => x).Must(x => x.From <= x.To).WithMessage("'from' must be earlier than 'to'.");
        }
    }
}
