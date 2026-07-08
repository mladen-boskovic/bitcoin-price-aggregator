using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class PriceProviderDto
    {
        public string ProviderName { get; set; } = string.Empty;
        public double Price { get; set; }
    }
}
