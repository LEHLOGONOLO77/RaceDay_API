namespace RaceDay.Api;

public static class ClaimsExtensions
{
    /// <summary>The Organiser.Id or Participant.Id stored in the session cookie at login.</summary>
    public static int GetProfileId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue("ProfileId")!);
}

public static class UserFactory
{
    public static string NormaliseEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>Builds a User plus its Organiser/Participant profile with a hashed password.</summary>
    public static User Create(IPasswordHasher<User> hasher, string email, string password, UserRole role,
        string name, string surname, string phone, DateOnly? dateOfBirth)
    {
        var user = new User { Email = NormaliseEmail(email), Role = role };
        user.PasswordHash = hasher.HashPassword(user, password);

        if (role == UserRole.Organiser)
            user.Organiser = new Organiser { Name = name, Surname = surname, Phone = phone };
        else
            user.Participant = new Participant
            {
                Name = name, Surname = surname, Phone = phone, DateOfBirth = dateOfBirth ?? default
            };
        return user;
    }
}
