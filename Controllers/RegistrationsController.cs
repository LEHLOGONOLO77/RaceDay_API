namespace RaceDay.Api;

[ApiController]
[Route("api")]
public class RegistrationsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public RegistrationsController(RaceDayDbContext db) => _db = db;

    private IQueryable<Registration> WithDetails() => _db.Registrations
        .Include(r => r.Participant)
        .Include(r => r.EventCategory).ThenInclude(c => c.Event);

    /// <summary>Participants see their own registrations; organisers see registrations for their own events.</summary>
    private bool CanAccess(Registration r) =>
        User.IsInRole("Organiser")
            ? r.EventCategory.Event.OrganiserId == User.GetProfileId()
            : r.ParticipantId == User.GetProfileId();

    /// <summary>Register the logged-in participant for an event category. Participant only.</summary>
    /// <remarks>The event must have status Open. Registering again after cancelling re-activates the registration.</remarks>
    /// <response code="201">Registration created.</response>
    /// <response code="400">Category does not exist or the event is not open.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a participant, or registering someone else.</response>
    /// <response code="409">Already registered for this category.</response>
    [Authorize(Roles = "Participant")]
    [HttpPost("registrations")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegistrationResponse>> Create(CreateRegistrationDto dto)
    {
        if (dto.ParticipantId != User.GetProfileId()) return Forbid();

        var cat = await _db.EventCategories.Include(c => c.Event).FirstOrDefaultAsync(c => c.Id == dto.EventCategoryId);
        if (cat is null) return BadRequest("Event category does not exist.");
        if (cat.Event.Status != EventStatus.Open)
            return BadRequest("Registration is only possible for events with status Open.");

        var reg = await _db.Registrations.FirstOrDefaultAsync(r =>
            r.ParticipantId == dto.ParticipantId && r.EventCategoryId == dto.EventCategoryId);

        if (reg is null)
        {
            reg = new Registration { ParticipantId = dto.ParticipantId, EventCategoryId = dto.EventCategoryId };
            _db.Registrations.Add(reg);
        }
        else if (reg.RegistrationStatus == RegistrationStatus.Cancelled)
        {
            reg.RegistrationStatus = RegistrationStatus.Pending;   // re-activate
            reg.PaymentStatus = PaymentStatus.Pending;
            reg.BibNumber = null;
            reg.RegistrationDate = DateTime.UtcNow;
        }
        else return Conflict("You are already registered for this category.");

        await _db.SaveChangesAsync();
        var full = await WithDetails().AsNoTracking().FirstAsync(r => r.Id == reg.Id);
        return CreatedAtAction(nameof(GetById), new { id = reg.Id }, RegistrationResponse.From(full));
    }

    /// <summary>Get one registration. Owner participant, or the organiser of the event.</summary>
    /// <response code="200">Registration details.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not your registration.</response>
    /// <response code="404">Registration not found.</response>
    [Authorize]
    [HttpGet("registrations/{id:int}")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegistrationResponse>> GetById(int id)
    {
        var reg = await WithDetails().AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (reg is null) return NotFound();
        if (!CanAccess(reg)) return Forbid();
        return RegistrationResponse.From(reg);
    }

    /// <summary>Get all registrations of a participant. Participants can only view their own.</summary>
    /// <response code="200">List of registrations.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a participant, or not your registrations.</response>
    [Authorize(Roles = "Participant")]
    [HttpGet("participants/{id:int}/registrations")]
    [ProducesResponseType(typeof(List<RegistrationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<RegistrationResponse>>> ForParticipant(int id)
    {
        if (id != User.GetProfileId()) return Forbid();
        var regs = await WithDetails().AsNoTracking().Where(r => r.ParticipantId == id)
            .OrderByDescending(r => r.RegistrationDate).ToListAsync();
        return regs.Select(RegistrationResponse.From).ToList();
    }

    /// <summary>Get all registrations for an event. Organiser only, own events only.</summary>
    /// <response code="200">List of registrations.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or not your event.</response>
    /// <response code="404">Event does not exist.</response>
    [Authorize(Roles = "Organiser")]
    [HttpGet("events/{eventId:int}/registrations")]
    [ProducesResponseType(typeof(List<RegistrationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<RegistrationResponse>>> ForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetProfileId()) return Forbid();

        var regs = await WithDetails().AsNoTracking().Where(r => r.EventCategory.EventId == eventId)
            .OrderBy(r => r.RegistrationDate).ToListAsync();
        return regs.Select(RegistrationResponse.From).ToList();
    }

    /// <summary>Update payment and registration status.</summary>
    /// <remarks>Organisers (of the event) may set any values. Participants may only mark their own registration Paid or Cancelled.</remarks>
    /// <response code="200">Registration updated.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not your registration, or a participant attempting a change only organisers may make.</response>
    /// <response code="404">Registration not found.</response>
    [Authorize]
    [HttpPut("registrations/{id:int}")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegistrationResponse>> Update(int id, UpdateRegistrationDto dto)
    {
        var reg = await WithDetails().FirstOrDefaultAsync(r => r.Id == id);
        if (reg is null) return NotFound();
        if (!CanAccess(reg)) return Forbid();
        if (!ApplyStatus(reg, dto.PaymentStatus, dto.RegistrationStatus)) return Forbid();

        await _db.SaveChangesAsync();
        return RegistrationResponse.From(reg);
    }

    /// <summary>Update the payment status after a participant has paid.</summary>
    /// <remarks>Marking a pending registration as Paid also confirms it. Participants may only set Paid.</remarks>
    /// <response code="200">Payment status updated.</response>
    /// <response code="400">Registration is cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not your registration, or a participant attempting a change only organisers may make.</response>
    /// <response code="404">Registration not found.</response>
    [Authorize]
    [HttpPut("registrations/{id:int}/payment")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegistrationResponse>> UpdatePayment(int id, UpdatePaymentDto dto)
    {
        var reg = await WithDetails().FirstOrDefaultAsync(r => r.Id == id);
        if (reg is null) return NotFound();
        if (!CanAccess(reg)) return Forbid();
        if (reg.RegistrationStatus == RegistrationStatus.Cancelled)
            return BadRequest("This registration has been cancelled.");
        if (!ApplyStatus(reg, dto.PaymentStatus, reg.RegistrationStatus)) return Forbid();

        await _db.SaveChangesAsync();
        return RegistrationResponse.From(reg);
    }

    /// <summary>Assign a bib number to a paid participant. Organiser only, own events only.</summary>
    /// <response code="200">Bib number assigned.</response>
    /// <response code="400">Participant has not paid, or the registration is cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an organiser, or not your event.</response>
    /// <response code="404">Registration not found.</response>
    /// <response code="409">Bib number already used in this event.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPut("registrations/{id:int}/bib-number")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegistrationResponse>> AssignBib(int id, AssignBibDto dto)
    {
        var reg = await WithDetails().FirstOrDefaultAsync(r => r.Id == id);
        if (reg is null) return NotFound();
        if (!CanAccess(reg)) return Forbid();
        if (reg.RegistrationStatus == RegistrationStatus.Cancelled)
            return BadRequest("This registration has been cancelled.");
        if (reg.PaymentStatus != PaymentStatus.Paid)
            return BadRequest("Participant has not paid.");

        var eventId = reg.EventCategory.EventId;
        var taken = await _db.Registrations.AnyAsync(r =>
            r.Id != id && r.BibNumber == dto.BibNumber && r.EventCategory.EventId == eventId);
        if (taken) return Conflict("That bib number is already used in this event.");

        reg.BibNumber = dto.BibNumber;
        await _db.SaveChangesAsync();
        return RegistrationResponse.From(reg);
    }

    /// <summary>Cancel a registration. The owner participant or the event's organiser.</summary>
    /// <remarks>The registration is kept with status Cancelled and its bib number is released.</remarks>
    /// <response code="204">Registration cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not your registration.</response>
    /// <response code="404">Registration not found.</response>
    [Authorize]
    [HttpDelete("registrations/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
    {
        var reg = await WithDetails().FirstOrDefaultAsync(r => r.Id == id);
        if (reg is null) return NotFound();
        if (!CanAccess(reg)) return Forbid();

        reg.RegistrationStatus = RegistrationStatus.Cancelled;
        reg.BibNumber = null;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Applies a status change. Participants are restricted so they cannot confirm themselves or un-cancel.</summary>
    private bool ApplyStatus(Registration reg, PaymentStatus payment, RegistrationStatus status)
    {
        if (User.IsInRole("Participant"))
        {
            if (payment != reg.PaymentStatus && payment != PaymentStatus.Paid) return false;
            if (status != reg.RegistrationStatus && status != RegistrationStatus.Cancelled) return false;
        }

        reg.PaymentStatus = payment;
        reg.RegistrationStatus = status;
        if (status == RegistrationStatus.Cancelled) reg.BibNumber = null;
        if (payment == PaymentStatus.Paid && status == RegistrationStatus.Pending)
            reg.RegistrationStatus = RegistrationStatus.Confirmed;
        return true;
    }
}
