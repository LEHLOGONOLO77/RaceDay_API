namespace RaceDay.Api;

public class CreateParticipantDto
{
    [Required] public string Name { get; set; } = "";
    [Required] public string Surname { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Required] public string Phone { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }
}

public class UpdateParticipantDto
{
    [Required] public string Name { get; set; } = "";
    [Required] public string Surname { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Phone { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }
}

public class UpdateOrganiserDto
{
    [Required] public string Name { get; set; } = "";
    [Required] public string Surname { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Phone { get; set; } = "";
}

public record ParticipantResponse(int Id, string Name, string Surname, string Email, string Phone, DateOnly DateOfBirth)
{
    public static ParticipantResponse From(Participant p) =>
        new(p.Id, p.Name, p.Surname, p.User.Email, p.Phone, p.DateOfBirth);
}

public record OrganiserResponse(int Id, string Name, string Surname, string Email, string Phone)
{
    public static OrganiserResponse From(Organiser o) =>
        new(o.Id, o.Name, o.Surname, o.User.Email, o.Phone);
}
