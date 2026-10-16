namespace RaceDay.Api;

[ApiController]
[Route("api")]
public class RoutesController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public RoutesController(RaceDayDbContext db) => _db = db;

    /// <summary>Create a route for an event. Organiser only, own events only.</summary>
    /// <response code="201">Route created.</response>
    /// <response code="400">Invalid data or event does not exist.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or not your event.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPost("routes")]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RouteResponse>> Create(CreateRouteDto dto)
    {
        var ev = await _db.Events.FindAsync(dto.EventId);
        if (ev is null) return BadRequest("Event does not exist.");
        if (ev.OrganiserId != User.GetProfileId()) return Forbid();

        var route = new EventRoute
        {
            EventId = dto.EventId, RouteName = dto.RouteName, Distance = dto.Distance,
            StartPoint = dto.StartPoint, FinishPoint = dto.FinishPoint, RouteDescription = dto.RouteDescription
        };
        _db.Routes.Add(route);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(ForEvent), new { eventId = route.EventId }, RouteResponse.From(route));
    }

    /// <summary>Get all routes for an event. Public.</summary>
    /// <response code="200">List of routes.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("events/{eventId:int}/routes")]
    [ProducesResponseType(typeof(List<RouteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<RouteResponse>>> ForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.Id == eventId)) return NotFound();
        var routes = await _db.Routes.AsNoTracking().Where(r => r.EventId == eventId).ToListAsync();
        return routes.Select(RouteResponse.From).ToList();
    }

    /// <summary>Update a route. Organiser only, own events only.</summary>
    /// <response code="200">Route updated.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or not your event.</response>
    /// <response code="404">Route not found.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPut("routes/{id:int}")]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RouteResponse>> Update(int id, UpdateRouteDto dto)
    {
        var route = await _db.Routes.Include(r => r.Event).FirstOrDefaultAsync(r => r.Id == id);
        if (route is null) return NotFound();
        if (route.Event.OrganiserId != User.GetProfileId()) return Forbid();

        route.RouteName = dto.RouteName; route.Distance = dto.Distance;
        route.StartPoint = dto.StartPoint; route.FinishPoint = dto.FinishPoint;
        route.RouteDescription = dto.RouteDescription;
        await _db.SaveChangesAsync();
        return RouteResponse.From(route);
    }

    /// <summary>Delete a route. Organiser only, own events only.</summary>
    /// <response code="204">Route deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or not your event.</response>
    /// <response code="404">Route not found.</response>
    [Authorize(Roles = "Organiser")]
    [HttpDelete("routes/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var route = await _db.Routes.Include(r => r.Event).FirstOrDefaultAsync(r => r.Id == id);
        if (route is null) return NotFound();
        if (route.Event.OrganiserId != User.GetProfileId()) return Forbid();

        _db.Routes.Remove(route);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
