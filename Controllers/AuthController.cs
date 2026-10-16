using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace RaceDay.Api;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher<User> _hasher;

    public AuthController(RaceDayDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    /// <summary>Register a new Organiser or Participant account.</summary>
    /// <remarks>Participants must supply a dateOfBirth. The password is stored hashed.</remarks>
    /// <response code="201">Account created.</response>
    /// <response code="400">Invalid data, invalid role, or missing date of birth.</response>
    /// <response code="409">Email already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (!Enum.IsDefined(dto.Role))
            return BadRequest("Role must be Organiser or Participant.");
        if (dto.Role == UserRole.Participant &&
            (dto.DateOfBirth is null || dto.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.UtcNow)))
            return BadRequest("A valid date of birth is required for participants.");

        var email = UserFactory.NormaliseEmail(dto.Email);
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict("Email already registered.");

        var user = UserFactory.Create(_hasher, dto.Email, dto.Password, dto.Role,
            dto.Name, dto.Surname, dto.Phone, dto.DateOfBirth);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();   // user + profile saved in one transaction

        var profileId = user.Role == UserRole.Organiser ? user.Organiser!.Id : user.Participant!.Id;
        return StatusCode(StatusCodes.Status201Created,
            new { UserId = user.Id, ProfileId = profileId, Role = user.Role.ToString() });
    }

    /// <summary>Log in and start a session (cookie).</summary>
    /// <response code="200">Logged in; session cookie issued.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = UserFactory.NormaliseEmail(dto.Email);
        var user = await _db.Users.Include(u => u.Organiser).Include(u => u.Participant)
            .FirstOrDefaultAsync(u => u.Email == email);

        // Same message for unknown email and wrong password, so emails can't be enumerated.
        if (user is null ||
            _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid email or password.");

        var profileId = user.Role == UserRole.Organiser ? user.Organiser!.Id : user.Participant!.Id;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("ProfileId", profileId.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identity));

        return Ok(new { UserId = user.Id, ProfileId = profileId, Role = user.Role.ToString() });
    }

    /// <summary>End the current session.</summary>
    /// <response code="204">Logged out.</response>
    /// <response code="401">Not logged in.</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return NoContent();
    }

    /// <summary>Show who the current session belongs to.</summary>
    /// <response code="200">Current user, role and profile id.</response>
    /// <response code="401">Not logged in.</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me() => Ok(new
    {
        UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        ProfileId = User.GetProfileId(),
        Email = User.FindFirstValue(ClaimTypes.Email),
        Role = User.FindFirstValue(ClaimTypes.Role)
    });
}
