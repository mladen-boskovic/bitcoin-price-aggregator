using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Constraints
{
    public static class SupportedInstruments
    {
        public static readonly HashSet<string> All = new (StringComparer.OrdinalIgnoreCase)
        {
            "BTCUSD"
        };
    }
}
