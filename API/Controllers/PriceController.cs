using Application.Constraints;
using Application.UseCases.Queries.GetAggregatedPrice;
using Application.UseCases.Queries.GetPriceHistory;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PriceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PriceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Get aggregated price for an instrument at a specific time.</summary>
        /// <param name="instrument">e.g. BTCUSD</param>
        /// <param name="timestampUtc">e.g. 2026-06-18T12:00:00Z</param>
        /// <param name="cancellationToken"></param>
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string instrument, [FromQuery] DateTime timestampUtc, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAggregatedPriceQuery(instrument, timestampUtc), cancellationToken);
            return Ok(result);
        }

        /// <summary>Get aggregated price history for a time range.</summary>
        /// <param name="from">e.g. 2024-06-18T12:00:00Z</param>
        /// <param name="to">e.g. 2026-06-18T12:00:00Z</param>
        /// <param name="cancellationToken"></param>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPriceHistoryQuery(from, to), cancellationToken);
            return Ok(result);
        }
    }
}
