using Application.Aggregation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Implementation.Aggregation
{
    public class AverageAggregationStrategy : IAggregationStrategy
    {
        public double Aggregate(IEnumerable<double> prices)
        {
            return prices.Average();
        }
    }
}
