using Implementation.Aggregation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.Strategies
{
    public class AverageAggregationStrategyTests
    {
        private readonly AverageAggregationStrategy _strategy = new();

        [Fact]
        public void Aggregate_WithMultiplePrices_ReturnsAverage()
        {
            var result = _strategy.Aggregate([50000.0, 51000.0]);
            Assert.Equal(50500.0, result);
        }

        [Fact]
        public void Aggregate_WithSinglePrice_ReturnsThatPrice()
        {
            var result = _strategy.Aggregate([50000.0]);
            Assert.Equal(50000.0, result);
        }
    }
}
