using Application.DTOs;
using Application.Repository;
using Application.UseCases.Queries.GetPriceHistory;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Implementation.UseCases.Queries
{
    public class GetPriceHistoryQueryHandler : IRequestHandler<GetPriceHistoryQuery, List<PriceHistoryDto>>
    {
        private readonly IPriceRepository _repository;

        public GetPriceHistoryQueryHandler(IPriceRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<PriceHistoryDto>> Handle(GetPriceHistoryQuery request, CancellationToken cancellationToken)
        {
            var from = new DateTime(request.From.Year, request.From.Month, request.From.Day, request.From.Hour, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(request.To.Year, request.To.Month, request.To.Day, request.To.Hour, 0, 0, DateTimeKind.Utc);

            var prices = await _repository.GetByRangeAsync(from, to, cancellationToken);

            return prices.Select(x => new PriceHistoryDto
            {
                TimestampUtc = x.TimestampUtc,
                AggregatedPrice = x.AggregatedPrice,
                ProviderPrices = x.ProviderPrices
            }).ToList();
        }
    }
}
