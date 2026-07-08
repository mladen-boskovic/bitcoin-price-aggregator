using Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.UseCases.Queries.GetPriceHistory
{
    public record GetPriceHistoryQuery(DateTime From, DateTime To) : IRequest<List<PriceHistoryDto>>;
}
