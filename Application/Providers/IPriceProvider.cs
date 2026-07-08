using Application.DTOs;
using Application.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Providers
{
    public interface IPriceProvider
    {
        string Name { get; }

        Task<PriceProviderDto> GetPriceAsync(string instrument, DateTime timestampUtc, CancellationToken cancellationToken = default);
    }
}
