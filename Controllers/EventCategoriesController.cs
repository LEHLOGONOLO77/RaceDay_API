namespace RaceDay.Api;

[ApiController]
[Route("api")]
public class EventCategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public EventCategoriesController(RaceDayDbContext db) => _db = db;

    /// <summary>Add a category (e.g. 10 km, 21 km, Under 20) to an event. Organiser only, own events only.</summary>
    /// <response code="201">Category created.</response>
    /// <response code="400">Invalid data or event does not exist.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or not your event.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPost("event-categories")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CategoryResponse>> Create(CreateCategoryDto dto)
    {
        var ev = await _db.Events.FindAsync(dto.EventId);
        if (ev is null) return BadRequest("Event does not exist.");
        if (ev.OrganiserId != User.GetProfileId()) return Forbid();

        var cat = new EventCategory
        {
            EventId = dto.EventId, CategoryName = dto.CategoryName,
            Distance = dto.Distance, EntryFee = dto.EntryFee
        };
        _db.EventCategories.Add(cat);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(ForEvent), new { eventId = cat.EventId }, CategoryResponse.From(cat));
    }

    /// <summary>Get all categories for an event. Public.</summary>
    /// <response code="200">List of categories.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CategoryResponse>>> ForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.Id == eventId)) return NotFound();
        var cats = await _db.EventCategories.AsNoTracking().Where(c => c.EventId == eventId).ToListAsync();
        return cats.Select(CategoryResponse.From).ToList();
    }

    /// <summary>Update a category. Organiser only, own events only.</summary>
    /// <response code="200">Category updated.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or not your event.</response>
    /// <response code="404">Category not found.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPut("event-categories/{id:int}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> Update(int id, UpdateCategoryDto dto)
    {
        var cat = await _db.EventCategories.Include(c => c.Event).FirstOrDefaultAsync(c => c.Id == id);
        if (cat is null) return NotFound();
        if (cat.Event.OrganiserId != User.GetProfileId()) return Forbid();

        cat.CategoryName = dto.CategoryName; cat.Distance = dto.Distance; cat.EntryFee = dto.EntryFee;
        await _db.SaveChangesAsync();
        return CategoryResponse.From(cat);
    }

    /// <summary>Delete a category. Organiser only, own events only.</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or not your event.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">People have already registered for this category.</response>
    [Authorize(Roles = "Organiser")]
    [HttpDelete("event-categories/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.EventCategories.Include(c => c.Event).FirstOrDefaultAsync(c => c.Id == id);
        if (cat is null) return NotFound();
        if (cat.Event.OrganiserId != User.GetProfileId()) return Forbid();
        if (await _db.Registrations.AnyAsync(r => r.EventCategoryId == id) ||
            await _db.Results.AnyAsync(r => r.EventCategoryId == id))
            return Conflict("People have already registered for this category.");

        _db.EventCategories.Remove(cat);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
