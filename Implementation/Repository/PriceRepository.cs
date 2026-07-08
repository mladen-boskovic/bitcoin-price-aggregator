using Application.Repository;
using DataAccess;
using Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Implementation.Repository
{
    public class PriceRepository : IPriceRepository
    {
        private readonly AppDbContext _dbContext;

        public PriceRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Price price, CancellationToken cancellationToken = default)
        {
            _dbContext.Prices.Add(price);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public Task<Price?> GetByInstrumentAndTimestampUtcAsync(string instrument, DateTime timestampUtc, CancellationToken cancellationToken = default)
        {
            return _dbContext.Prices.AsNoTracking()
                .FirstOrDefaultAsync(x => x.TimestampUtc == timestampUtc && x.Instrument.ToLower() == instrument.ToLower(), cancellationToken);
        }

        public Task<List<Price>> GetByRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
        {
            return _dbContext.Prices.AsNoTracking()
                .Where(x => x.TimestampUtc >= from && x.TimestampUtc <= to).OrderBy(x => x.TimestampUtc).ToListAsync(cancellationToken);
        }
    }
}
