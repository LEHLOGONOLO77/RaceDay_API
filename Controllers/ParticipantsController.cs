namespace RaceDay.Api;

[ApiController]
[Route("api")]
public class ParticipantsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher<User> _hasher;

    public ParticipantsController(RaceDayDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    /// <summary>Register a new participant account. Public.</summary>
    /// <remarks>Convenience endpoint from the Part 1 plan. Same as POST /api/auth/register with role Participant.</remarks>
    /// <response code="201">Participant created.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="409">Email already registered.</response>
    [HttpPost("participants")]
    [ProducesResponseType(typeof(ParticipantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ParticipantResponse>> Create(CreateParticipantDto dto)
    {
        if (dto.DateOfBirth == default || dto.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
            return BadRequest("A valid date of birth is required.");

        var email = UserFactory.NormaliseEmail(dto.Email);
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict("Email already registered.");

        var user = UserFactory.Create(_hasher, dto.Email, dto.Password, UserRole.Participant,
            dto.Name, dto.Surname, dto.Phone, dto.DateOfBirth);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Participant!.Id }, ParticipantResponse.From(user.Participant));
    }

    /// <summary>Get a participant's profile. A participant can view only their own; an organiser can view participants registered for their events.</summary>
    /// <response code="200">Participant details.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not allowed to view this participant.</response>
    /// <response code="404">Participant does not exist.</response>
    [Authorize]
    [HttpGet("participants/{id:int}")]
    [ProducesResponseType(typeof(ParticipantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParticipantResponse>> GetById(int id)
    {
        var p = await _db.Participants.Include(x => x.User).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        if (!await CanAccess(id)) return Forbid();
        return ParticipantResponse.From(p);
    }

    /// <summary>Update your own participant profile. Participant only.</summary>
    /// <response code="200">Profile updated.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a participant, or not your profile.</response>
    /// <response code="404">Participant does not exist.</response>
    /// <response code="409">Email already used by another account.</response>
    [Authorize(Roles = "Participant")]
    [HttpPut("participants/{id:int}")]
    [ProducesResponseType(typeof(ParticipantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ParticipantResponse>> Update(int id, UpdateParticipantDto dto)
    {
        if (id != User.GetProfileId()) return Forbid();
        var p = await _db.Participants.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        if (dto.DateOfBirth == default || dto.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
            return BadRequest("A valid date of birth is required.");

        var email = UserFactory.NormaliseEmail(dto.Email);
        if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != p.UserId))
            return Conflict("Email already used by another account.");

        p.Name = dto.Name; p.Surname = dto.Surname; p.Phone = dto.Phone;
        p.DateOfBirth = dto.DateOfBirth; p.User.Email = email;
        await _db.SaveChangesAsync();
        return ParticipantResponse.From(p);
    }

    /// <summary>Delete a participant account, including their registrations and results. A participant can delete only themselves; an organiser only participants registered for their events.</summary>
    /// <response code="204">Participant deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not allowed to delete this participant.</response>
    /// <response code="404">Participant does not exist.</response>
    [Authorize]
    [HttpDelete("participants/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Participants.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        if (!await CanAccess(id)) return Forbid();

        _db.Results.RemoveRange(_db.Results.Where(r => r.ParticipantId == id));
        _db.Registrations.RemoveRange(_db.Registrations.Where(r => r.ParticipantId == id));
        _db.Users.Remove(p.User);   // participant profile cascades from the user
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> CanAccess(int participantId)
    {
        var profileId = User.GetProfileId();
        if (User.IsInRole("Participant")) return participantId == profileId;
        return await _db.Registrations.AnyAsync(r =>
            r.ParticipantId == participantId && r.EventCategory.Event.OrganiserId == profileId);
    }
}
