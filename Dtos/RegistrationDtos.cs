namespace RaceDay.Api;

public class CreateRegistrationDto
{
    /// <summary>Must be the logged-in participant's own ID.</summary>
    public int ParticipantId { get; set; }
    public int EventCategoryId { get; set; }
}

public class UpdateRegistrationDto
{
    [EnumDataType(typeof(PaymentStatus))] public PaymentStatus PaymentStatus { get; set; }
    [EnumDataType(typeof(RegistrationStatus))] public RegistrationStatus RegistrationStatus { get; set; }
}

public class UpdatePaymentDto
{
    [EnumDataType(typeof(PaymentStatus))] public PaymentStatus PaymentStatus { get; set; }
}

public class AssignBibDto
{
    [Range(1, 99999)] public int BibNumber { get; set; }
}

public record RegistrationResponse(int Id, int ParticipantId, string ParticipantName, int EventId, string EventName,
    int EventCategoryId, string CategoryName, DateTime RegistrationDate, string PaymentStatus,
    string RegistrationStatus, int? BibNumber)
{
    public static RegistrationResponse From(Registration r) => new(r.Id, r.ParticipantId,
        $"{r.Participant.Name} {r.Participant.Surname}", r.EventCategory.EventId, r.EventCategory.Event.Name,
        r.EventCategoryId, r.EventCategory.CategoryName, r.RegistrationDate,
        r.PaymentStatus.ToString(), r.RegistrationStatus.ToString(), r.BibNumber);
}
