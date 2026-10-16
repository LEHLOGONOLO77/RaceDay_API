namespace RaceDay.Api;

public class Event
{
    public int Id { get; set; }
    public int OrganiserId { get; set; }
    public Organiser Organiser { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public decimal Distance { get; set; }
    public EventType EventType { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Draft;
    public ICollection<EventRoute> Routes { get; set; } = new List<EventRoute>();
    public ICollection<EventCategory> Categories { get; set; } = new List<EventCategory>();
}

// Named EventRoute to avoid clashing with ASP.NET's Route attribute/type.
public class EventRoute
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string RouteName { get; set; } = "";
    public decimal Distance { get; set; }
    public string StartPoint { get; set; } = "";
    public string FinishPoint { get; set; } = "";
    public string RouteDescription { get; set; } = "";
}

public class EventCategory
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string CategoryName { get; set; } = "";
    public decimal Distance { get; set; }
    public decimal EntryFee { get; set; }
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}

/// <summary>Links a Participant to an EventCategory (and therefore an Event).</summary>
public class Registration
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;
    public int EventCategoryId { get; set; }
    public EventCategory EventCategory { get; set; } = null!;
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public RegistrationStatus RegistrationStatus { get; set; } = RegistrationStatus.Pending;
    public int? BibNumber { get; set; }
}

public class Result
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public int EventCategoryId { get; set; }
    public EventCategory EventCategory { get; set; } = null!;
    public int Position { get; set; }
    public TimeSpan FinishTime { get; set; }
}
