namespace RaceDay.Api;

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public ResultsController(RaceDayDbContext db) => _db = db;

    private IQueryable<Result> WithDetails() => _db.Results
        .Include(r => r.Participant).Include(r => r.Event).Include(r => r.EventCategory);

    /// <summary>Record a participant's result. Organiser only, own events only.</summary>
    /// <remarks>The participant must hold a non-cancelled registration for the given category, and the category must belong to the event.</remarks>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Invalid data, category not in event, or participant not registered.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or not your event.</response>
    /// <response code="409">Participant already has a result, or the position is taken in that category.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPost("results")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultResponse>> Create(CreateResultDto dto)
    {
        if (dto.FinishTime <= TimeSpan.Zero) return BadRequest("Finish time must be greater than zero.");

        var ev = await _db.Events.FindAsync(dto.EventId);
        if (ev is null) return BadRequest("Event does not exist.");
        if (ev.OrganiserId != User.GetProfileId()) return Forbid();

        if (!await _db.EventCategories.AnyAsync(c => c.Id == dto.EventCategoryId && c.EventId == dto.EventId))
            return BadRequest("Category does not belong to this event.");

        var registered = await _db.Registrations.AnyAsync(r =>
            r.ParticipantId == dto.ParticipantId && r.EventCategoryId == dto.EventCategoryId &&
            r.RegistrationStatus != RegistrationStatus.Cancelled);
        if (!registered) return BadRequest("Participant is not registered for this category.");

        if (await _db.Results.AnyAsync(r => r.ParticipantId == dto.ParticipantId && r.EventId == dto.EventId))
            return Conflict("This participant already has a result for this event.");
        if (await _db.Results.AnyAsync(r => r.EventCategoryId == dto.EventCategoryId && r.Position == dto.Position))
            return Conflict("That position is already taken in this category.");

        var result = new Result
        {
            ParticipantId = dto.ParticipantId, EventId = dto.EventId, EventCategoryId = dto.EventCategoryId,
            Position = dto.Position, FinishTime = dto.FinishTime
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var full = await WithDetails().AsNoTracking().FirstAsync(r => r.Id == result.Id);
        return CreatedAtAction(nameof(ForEvent), new { eventId = result.EventId }, ResultResponse.From(full));
    }

    /// <summary>Get all results for an event, ordered by category then position. Public.</summary>
    /// <response code="200">List of results.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("events/{eventId:int}/results")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ResultResponse>>> ForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.Id == eventId)) return NotFound();
        var results = await WithDetails().AsNoTracking().Where(r => r.EventId == eventId)
            .OrderBy(r => r.EventCategoryId).ThenBy(r => r.Position).ToListAsync();
        return results.Select(ResultResponse.From).ToList();
    }

    /// <summary>Get a participant's own race results. Participants can only view their own.</summary>
    /// <response code="200">List of results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a participant, or not your results.</response>
    [Authorize(Roles = "Participant")]
    [HttpGet("participants/{id:int}/results")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<ResultResponse>>> ForParticipant(int id)
    {
        if (id != User.GetProfileId()) return Forbid();
        var results = await WithDetails().AsNoTracking().Where(r => r.ParticipantId == id)
            .OrderByDescending(r => r.Event.EventDate).ToListAsync();
        return results.Select(ResultResponse.From).ToList();
    }

    /// <summary>Update a result. Organiser only, own events only.</summary>
    /// <response code="200">Result updated.</response>
    /// <response code="400">Invalid finish time.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or not your event.</response>
    /// <response code="404">Result does not exist.</response>
    /// <response code="409">Position already taken in this category.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPut("results/{id:int}")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultResponse>> Update(int id, UpdateResultDto dto)
    {
        if (dto.FinishTime <= TimeSpan.Zero) return BadRequest("Finish time must be greater than zero.");

        var result = await WithDetails().FirstOrDefaultAsync(r => r.Id == id);
        if (result is null) return NotFound();
        if (result.Event.OrganiserId != User.GetProfileId()) return Forbid();

        if (await _db.Results.AnyAsync(r => r.Id != id && r.EventCategoryId == result.EventCategoryId && r.Position == dto.Position))
            return Conflict("That position is already taken in this category.");

        result.Position = dto.Position;
        result.FinishTime = dto.FinishTime;
        await _db.SaveChangesAsync();
        return ResultResponse.From(result);
    }

    /// <summary>Delete an incorrect result. Organiser only, own events only.</summary>
    /// <response code="204">Result deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or not your event.</response>
    /// <response code="404">Result does not exist.</response>
    [Authorize(Roles = "Organiser")]
    [HttpDelete("results/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await WithDetails().FirstOrDefaultAsync(r => r.Id == id);
        if (result is null) return NotFound();
        if (result.Event.OrganiserId != User.GetProfileId()) return Forbid();

        _db.Results.Remove(result);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
