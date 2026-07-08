using Application.DTOs;
using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Repository
{
    public interface IPriceRepository
    {
        Task<Price?> GetByInstrumentAndTimestampUtcAsync(string instrument, DateTime timestampUtc, CancellationToken cancellationToken = default);

        Task AddAsync(Price price, CancellationToken cancellationToken = default);

        Task<List<Price>> GetByRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    }
}
