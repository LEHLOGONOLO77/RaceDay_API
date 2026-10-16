namespace RaceDay.Api;

[ApiController]
[Route("api")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public EventsController(RaceDayDbContext db) => _db = db;

    /// <summary>Get all events. Public.</summary>
    /// <response code="200">List of events, soonest first.</response>
    [HttpGet("events")]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventResponse>>> GetAll()
    {
        var events = await _db.Events.AsNoTracking().OrderBy(e => e.EventDate).ToListAsync();
        return events.Select(EventResponse.From).ToList();
    }

    /// <summary>Get one event by ID. Public.</summary>
    /// <response code="200">Event details.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("events/{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetById(int id)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        return ev is null ? NotFound() : EventResponse.From(ev);
    }

    /// <summary>Create an event. Organiser only; the event is owned by the logged-in organiser.</summary>
    /// <response code="201">Event created.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPost("events")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventResponse>> Create(EventDto dto)
    {
        var ev = new Event
        {
            OrganiserId = User.GetProfileId(),   // from the session, never from the request body
            Name = dto.Name, Description = dto.Description, EventDate = dto.EventDate,
            Location = dto.Location, Distance = dto.Distance,
            EventType = dto.EventType, Status = dto.Status
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = ev.Id }, EventResponse.From(ev));
    }

    /// <summary>Update an event. Organiser only, and only their own events.</summary>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or not your event.</response>
    /// <response code="404">Event does not exist.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPut("events/{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> Update(int id, EventDto dto)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetProfileId()) return Forbid();

        ev.Name = dto.Name; ev.Description = dto.Description; ev.EventDate = dto.EventDate;
        ev.Location = dto.Location; ev.Distance = dto.Distance;
        ev.EventType = dto.EventType; ev.Status = dto.Status;
        await _db.SaveChangesAsync();
        return EventResponse.From(ev);
    }

    /// <summary>Delete an event (and its routes and categories). Organiser only, own events only.</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or not your event.</response>
    /// <response code="404">Event does not exist.</response>
    /// <response code="409">Event already has registrations or results.</response>
    [Authorize(Roles = "Organiser")]
    [HttpDelete("events/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetProfileId()) return Forbid();

        var inUse = await _db.Registrations.AnyAsync(r => r.EventCategory.EventId == id)
                 || await _db.Results.AnyAsync(r => r.EventId == id);
        if (inUse) return Conflict("This event has registrations or results and cannot be deleted.");

        _db.Events.Remove(ev);   // routes and categories cascade
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Get all events created by an organiser. An organiser can only list their own.</summary>
    /// <response code="200">The organiser's events.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or requesting another organiser's list.</response>
    [Authorize(Roles = "Organiser")]
    [HttpGet("organisers/{id:int}/events")]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<EventResponse>>> ForOrganiser(int id)
    {
        if (id != User.GetProfileId()) return Forbid();
        var events = await _db.Events.AsNoTracking().Where(e => e.OrganiserId == id)
            .OrderBy(e => e.EventDate).ToListAsync();
        return events.Select(EventResponse.From).ToList();
    }
}
