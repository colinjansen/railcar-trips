using Microsoft.AspNetCore.Mvc;
using RailcarTrips.Application.UseCases;
using RailcarTrips.Shared.Dtos;

namespace RailcarTrips.Server.Controllers;

[ApiController]
[Route("api/railcartrips")]
public sealed class RailcarTripsController(ProcessTripsUseCase processTripsUseCase, TripQueryService tripQueryService) : ControllerBase
{
    private readonly ProcessTripsUseCase _processTripsUseCase = processTripsUseCase;
    private readonly TripQueryService _tripQueryService = tripQueryService;

    [HttpPost("process")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<ProcessResultDto>> Process([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Please upload a CSV file.");
        }

        await using var stream = file.OpenReadStream();
        var result = await _processTripsUseCase.Execute(stream, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<TripDto>>> GetTrips(CancellationToken cancellationToken)
    {
        var trips = await _tripQueryService.GetTrips(cancellationToken);

        return Ok(trips);
    }

    [HttpGet("{tripId:int}/events")]
    public async Task<ActionResult<List<TripEventDto>>> GetTripEvents(int tripId, CancellationToken cancellationToken)
    {
        var events = await _tripQueryService.GetTripEvents(tripId, cancellationToken);

        if (events.Count == 0)
        {
            return NotFound();
        }

        return Ok(events);
    }
}
