namespace RaceDay.Api;

[ApiController]
[Route("api")]
public class OrganisersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public OrganisersController(RaceDayDbContext db) => _db = db;

    /// <summary>Get your own organiser profile. Organiser only.</summary>
    /// <response code="200">Organiser details.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or not your profile.</response>
    /// <response code="404">Organiser does not exist.</response>
    [Authorize(Roles = "Organiser")]
    [HttpGet("organisers/{id:int}")]
    [ProducesResponseType(typeof(OrganiserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrganiserResponse>> GetById(int id)
    {
        if (id != User.GetProfileId()) return Forbid();
        var o = await _db.Organisers.Include(x => x.User).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return o is null ? NotFound() : OrganiserResponse.From(o);
    }

    /// <summary>Update your own organiser profile. Organiser only.</summary>
    /// <response code="200">Profile updated.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or not your profile.</response>
    /// <response code="404">Organiser does not exist.</response>
    /// <response code="409">Email already used by another account.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPut("organisers/{id:int}")]
    [ProducesResponseType(typeof(OrganiserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrganiserResponse>> Update(int id, UpdateOrganiserDto dto)
    {
        if (id != User.GetProfileId()) return Forbid();
        var o = await _db.Organisers.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
        if (o is null) return NotFound();

        var email = UserFactory.NormaliseEmail(dto.Email);
        if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != o.UserId))
            return Conflict("Email already used by another account.");

        o.Name = dto.Name; o.Surname = dto.Surname; o.Phone = dto.Phone; o.User.Email = email;
        await _db.SaveChangesAsync();
        return OrganiserResponse.From(o);
    }
}
