using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Aggregation
{
    public interface IAggregationStrategy
    {
        double Aggregate(IEnumerable<double> prices);
    }
}
