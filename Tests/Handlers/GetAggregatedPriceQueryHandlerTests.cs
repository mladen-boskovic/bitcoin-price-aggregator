using Application.Aggregation;
using Application.DTOs;
using Application.Enums;
using Application.Providers;
using Application.Repository;
using Application.UseCases.Queries.GetAggregatedPrice;
using Domain;
using Implementation.Caching;
using Implementation.UseCases.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.Handlers
{
    public class GetAggregatedPriceQueryHandlerTests
    {
        private readonly Mock<IPriceRepository> _repositoryMock = new();
        private readonly Mock<IAggregationStrategy> _aggregationStrategyMock = new();
        private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
        private readonly Mock<IPriceProvider> _bitstampMock = new();
        private readonly Mock<IPriceProvider> _bitfinexMock = new();
        private readonly GetAggregatedPriceQueryHandler _handler;

        private const string Instrument = "BTCUSD";
        private static readonly DateTime Timestamp = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        public GetAggregatedPriceQueryHandlerTests()
        {
            _bitstampMock.Setup(x => x.Name).Returns("Bitstamp");
            _bitfinexMock.Setup(x => x.Name).Returns("Bitfinex");

            _aggregationStrategyMock
                .Setup(x => x.Aggregate(It.IsAny<IEnumerable<double>>()))
                .Returns<IEnumerable<double>>(prices => prices.Average());

            _handler = new GetAggregatedPriceQueryHandler(
                [_bitstampMock.Object, _bitfinexMock.Object],
                _repositoryMock.Object,
                _aggregationStrategyMock.Object,
                _memoryCache
            );
        }

        [Fact]
        public async Task Handle_WhenMemoryCacheHit_ReturnsMemoryCacheSource()
        {
            var cachedDto = new PriceResponseDto
            {
                Instrument = Instrument,
                TimestampUtc = Timestamp,
                AggregatedPrice = 50000,
                ProviderPrices = new Dictionary<string, double> { { "Bitstamp", 50000 } },
                Source = PriceSource.MemoryCache
            };

            _memoryCache.Set(PriceCache.ForPrice(Instrument, Timestamp), cachedDto);

            var result = await _handler.Handle(new GetAggregatedPriceQuery(Instrument, Timestamp), CancellationToken.None);

            Assert.Equal(PriceSource.MemoryCache, result.Source);
            Assert.Equal(50000, result.AggregatedPrice);
            _repositoryMock.Verify(x => x.GetByInstrumentAndTimestampUtcAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenDbCacheHit_ReturnsDbCacheSourceAndPopulatesMemoryCache()
        {
            var dbPrice = new Price
            {
                Instrument = Instrument,
                TimestampUtc = Timestamp,
                AggregatedPrice = 50000,
                ProviderPrices = new Dictionary<string, double> { { "Bitstamp", 50000 } }
            };

            _repositoryMock
                .Setup(x => x.GetByInstrumentAndTimestampUtcAsync(Instrument, Timestamp, It.IsAny<CancellationToken>()))
                .ReturnsAsync(dbPrice);

            var result = await _handler.Handle(new GetAggregatedPriceQuery(Instrument, Timestamp), CancellationToken.None);

            Assert.Equal(PriceSource.DbCache, result.Source);
            Assert.Equal(50000, result.AggregatedPrice);

            // memory cache should now be populated
            var cached = _memoryCache.Get<PriceResponseDto>(PriceCache.ForPrice(Instrument, Timestamp));
            Assert.NotNull(cached);
            Assert.Equal(PriceSource.MemoryCache, cached.Source);
        }

        [Fact]
        public async Task Handle_WhenNoCacheHit_FetchesFromProvidersAndReturnsExternalAPIsSource()
        {
            _repositoryMock
                .Setup(x => x.GetByInstrumentAndTimestampUtcAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Price?)null);

            _bitstampMock
                .Setup(x => x.GetPriceAsync(Instrument, Timestamp, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PriceProviderDto { ProviderName = "Bitstamp", Price = 50000 });

            _bitfinexMock
                .Setup(x => x.GetPriceAsync(Instrument, Timestamp, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PriceProviderDto { ProviderName = "Bitfinex", Price = 51000 });

            var result = await _handler.Handle(new GetAggregatedPriceQuery(Instrument, Timestamp), CancellationToken.None);

            Assert.Equal(PriceSource.ExternalAPIs, result.Source);
            Assert.Equal(50500, result.AggregatedPrice);
            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Price>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenOneProviderFails_AggregatesRemainingProviders()
        {
            _repositoryMock
                .Setup(x => x.GetByInstrumentAndTimestampUtcAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Price?)null);

            _bitstampMock
                .Setup(x => x.GetPriceAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Bitstamp unavailable"));

            _bitfinexMock
                .Setup(x => x.GetPriceAsync(Instrument, Timestamp, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PriceProviderDto { ProviderName = "Bitfinex", Price = 51000 });

            var result = await _handler.Handle(new GetAggregatedPriceQuery(Instrument, Timestamp), CancellationToken.None);

            Assert.Equal(PriceSource.ExternalAPIs, result.Source);
            Assert.Equal(51000, result.AggregatedPrice);
            Assert.False(result.ProviderPrices.ContainsKey("Bitstamp"));
        }

        [Fact]
        public async Task Handle_WhenAllProvidersFail_ThrowsInvalidOperationException()
        {
            _repositoryMock
                .Setup(x => x.GetByInstrumentAndTimestampUtcAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Price?)null);

            _bitstampMock
                .Setup(x => x.GetPriceAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException());

            _bitfinexMock
                .Setup(x => x.GetPriceAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _handler.Handle(new GetAggregatedPriceQuery(Instrument, Timestamp), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenRaceConditionOccurs_ReturnsExistingRecordFromDb()
        {
            _repositoryMock
                .SetupSequence(x => x.GetByInstrumentAndTimestampUtcAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Price?)null)
                .ReturnsAsync(new Price
                {
                    Instrument = Instrument,
                    TimestampUtc = Timestamp,
                    AggregatedPrice = 50000,
                    ProviderPrices = new Dictionary<string, double> { { "Bitfinex", 50000 } }
                });

            _bitstampMock
                .Setup(x => x.GetPriceAsync(Instrument, Timestamp, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PriceProviderDto { ProviderName = "Bitstamp", Price = 50000 });

            _bitfinexMock
                .Setup(x => x.GetPriceAsync(Instrument, Timestamp, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PriceProviderDto { ProviderName = "Bitfinex", Price = 50000 });

            _repositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Price>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("Unique constraint", new Exception()));

            var result = await _handler.Handle(new GetAggregatedPriceQuery(Instrument, Timestamp), CancellationToken.None);

            Assert.Equal(PriceSource.DbCache, result.Source);
        }
    }
}
