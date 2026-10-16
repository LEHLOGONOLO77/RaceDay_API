namespace RaceDay.Api;

/// <summary>Login identity. Each user has exactly one profile (Organiser or Participant).</summary>
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; }
    public Organiser? Organiser { get; set; }
    public Participant? Participant { get; set; }
}

public class Organiser
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Surname { get; set; } = "";
    public string Phone { get; set; } = "";
    public ICollection<Event> Events { get; set; } = new List<Event>();
}

public class Participant
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Surname { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
