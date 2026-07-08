using Application.Constraints;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.UseCases.Queries.GetAggregatedPrice
{
    public class GetAggregatedPriceQueryValidator : AbstractValidator<GetAggregatedPriceQuery>
    {
        public GetAggregatedPriceQueryValidator()
        {
            RuleFor(x => x.Instrument)
            .NotEmpty().WithMessage("Instrument is required.")
            .Must(i => SupportedInstruments.All.Contains(i))
            .WithMessage(x => $"Instrument '{x.Instrument}' is not supported. " +
                              $"Supported instruments: {string.Join(", ", SupportedInstruments.All)}.");

            RuleFor(x => x.TimestampUtc)
                .NotEqual(default(DateTime)).WithMessage("Timestamp is required.")
                .LessThanOrEqualTo(_ => DateTime.UtcNow).WithMessage("Timestamp cannot be in the future.");
        }
    }
}
