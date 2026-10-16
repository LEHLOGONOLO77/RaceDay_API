namespace RaceDay.Api;

public class RegisterDto
{
    [Required] public string Name { get; set; } = "";
    [Required] public string Surname { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Required] public string Phone { get; set; } = "";
    /// <summary>Organiser or Participant.</summary>
    [EnumDataType(typeof(UserRole))] public UserRole Role { get; set; }
    /// <summary>Required for Participants (format yyyy-MM-dd).</summary>
    public DateOnly? DateOfBirth { get; set; }
}

public class LoginDto
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}
