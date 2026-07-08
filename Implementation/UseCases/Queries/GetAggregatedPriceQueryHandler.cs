using Application.Aggregation;
using Application.DTOs;
using Application.Enums;
using Application.Providers;
using Application.Repository;
using Application.UseCases.Queries.GetAggregatedPrice;
using Domain;
using Implementation.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Text;

namespace Implementation.UseCases.Queries
{
    public class GetAggregatedPriceQueryHandler : IRequestHandler<GetAggregatedPriceQuery, PriceResponseDto>
    {
        private readonly IEnumerable<IPriceProvider> _priceProviders;
        private readonly IPriceRepository _repository;
        private readonly IAggregationStrategy _aggregationStrategy;
        private readonly IMemoryCache _memoryCache;

        public GetAggregatedPriceQueryHandler(IEnumerable<IPriceProvider> priceProviders, IPriceRepository repository, IAggregationStrategy aggregationStrategy, IMemoryCache memoryCache)
        {
            _priceProviders = priceProviders;
            _repository = repository;
            _aggregationStrategy = aggregationStrategy;
            _memoryCache = memoryCache;
        }

        public async Task<PriceResponseDto> Handle(GetAggregatedPriceQuery request, CancellationToken cancellationToken)
        {
            var timestampUtc = request.TimestampUtc;
            timestampUtc = new DateTime(timestampUtc.Year, timestampUtc.Month, timestampUtc.Day, timestampUtc.Hour, 0, 0, DateTimeKind.Utc);

            var cacheKey = PriceCache.ForPrice(request.Instrument, timestampUtc);

            var memoryCached = _memoryCache.Get<PriceResponseDto>(cacheKey);
            if (memoryCached != null)
            {
                return memoryCached;
            }

            var cachedPrice = await _repository.GetByInstrumentAndTimestampUtcAsync(request.Instrument, timestampUtc, cancellationToken);
            if (cachedPrice != null)
            {
                var dto = new PriceResponseDto
                {
                    Instrument = cachedPrice.Instrument,
                    TimestampUtc = timestampUtc,
                    AggregatedPrice = cachedPrice.AggregatedPrice,
                    ProviderPrices = cachedPrice.ProviderPrices,
                    Source = PriceSource.DbCache
                };

                _memoryCache.Set(cacheKey, dto with { Source = PriceSource.MemoryCache }, PriceCache.DefaultEntryOptions);
                return dto;
            }

            var tasks = _priceProviders.Select(async p =>
            {
                try
                {
                    return (PriceProviderDto?)await p.GetPriceAsync(request.Instrument, timestampUtc, cancellationToken);
                }
                catch
                {
                    return null;
                }
            });

            var results = (await Task.WhenAll(tasks)).Where(r => r is not null).Select(r => r!).ToList();
            if (results.Count == 0)
            {
                throw new InvalidOperationException("All price providers failed to return data.");
            }

            var providerPrices = results.ToDictionary(r => r.ProviderName, r => r.Price);
            var aggregated = _aggregationStrategy.Aggregate(results.Select(x => x.Price));

            var entity = new Price
            {
                Instrument = request.Instrument,
                TimestampUtc = timestampUtc,
                AggregatedPrice = aggregated,
                ProviderPrices = providerPrices
            };

            try
            {
                await _repository.AddAsync(entity, cancellationToken);
            }
            catch (DbUpdateException)
            {
                // A concurrent request already inserted this record (unique constraint violation).
                // Re-read from DB and return the existing entry.
                var existingPrice = await _repository.GetByInstrumentAndTimestampUtcAsync(request.Instrument, timestampUtc, cancellationToken);
                if (existingPrice != null)
                {
                    var dto = new PriceResponseDto
                    {
                        Instrument = existingPrice.Instrument,
                        TimestampUtc = timestampUtc,
                        AggregatedPrice = existingPrice.AggregatedPrice,
                        ProviderPrices = existingPrice.ProviderPrices,
                        Source = PriceSource.DbCache
                    };

                    _memoryCache.Set(cacheKey, dto with { Source = PriceSource.MemoryCache }, PriceCache.DefaultEntryOptions);
                    return dto;
                }

                throw;
            }

            var responseDto = new PriceResponseDto
            {
                Instrument = request.Instrument,
                TimestampUtc = timestampUtc,
                AggregatedPrice = aggregated,
                ProviderPrices = providerPrices,
                Source = PriceSource.ExternalAPIs
            };

            _memoryCache.Set(cacheKey, responseDto with { Source = PriceSource.MemoryCache }, PriceCache.DefaultEntryOptions);
            return responseDto;
        }
    }
}
