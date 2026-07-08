using Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.UseCases.Queries.GetAggregatedPrice
{
    public record GetAggregatedPriceQuery(string Instrument, DateTime TimestampUtc) : IRequest<PriceResponseDto>;
}
