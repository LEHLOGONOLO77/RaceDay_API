namespace RaceDay.Api;

/// <summary>Used for both creating and updating an event.</summary>
public class EventDto
{
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [Required] public string Description { get; set; } = "";
    public DateTime EventDate { get; set; }
    [Required] public string Location { get; set; } = "";
    [Range(0.1, 1000)] public decimal Distance { get; set; }
    /// <summary>Run, Walk or Cycle.</summary>
    [EnumDataType(typeof(EventType))] public EventType EventType { get; set; }
    /// <summary>Draft, Open, Closed, Completed or Cancelled. Participants can only register for Open events.</summary>
    [EnumDataType(typeof(EventStatus))] public EventStatus Status { get; set; } = EventStatus.Draft;
}

public record EventResponse(int Id, int OrganiserId, string Name, string Description,
    DateTime EventDate, string Location, decimal Distance, string EventType, string Status)
{
    public static EventResponse From(Event e) => new(e.Id, e.OrganiserId, e.Name, e.Description,
        e.EventDate, e.Location, e.Distance, e.EventType.ToString(), e.Status.ToString());
}

public class CreateRouteDto
{
    public int EventId { get; set; }
    [Required] public string RouteName { get; set; } = "";
    [Range(0.1, 1000)] public decimal Distance { get; set; }
    [Required] public string StartPoint { get; set; } = "";
    [Required] public string FinishPoint { get; set; } = "";
    public string RouteDescription { get; set; } = "";
}

public class UpdateRouteDto
{
    [Required] public string RouteName { get; set; } = "";
    [Range(0.1, 1000)] public decimal Distance { get; set; }
    [Required] public string StartPoint { get; set; } = "";
    [Required] public string FinishPoint { get; set; } = "";
    public string RouteDescription { get; set; } = "";
}

public record RouteResponse(int Id, int EventId, string RouteName, decimal Distance,
    string StartPoint, string FinishPoint, string RouteDescription)
{
    public static RouteResponse From(EventRoute r) => new(r.Id, r.EventId, r.RouteName,
        r.Distance, r.StartPoint, r.FinishPoint, r.RouteDescription);
}

public class CreateCategoryDto
{
    public int EventId { get; set; }
    [Required] public string CategoryName { get; set; } = "";
    [Range(0.1, 1000)] public decimal Distance { get; set; }
    [Range(0, 100000)] public decimal EntryFee { get; set; }
}

public class UpdateCategoryDto
{
    [Required] public string CategoryName { get; set; } = "";
    [Range(0.1, 1000)] public decimal Distance { get; set; }
    [Range(0, 100000)] public decimal EntryFee { get; set; }
}

public record CategoryResponse(int Id, int EventId, string CategoryName, decimal Distance, decimal EntryFee)
{
    public static CategoryResponse From(EventCategory c) =>
        new(c.Id, c.EventId, c.CategoryName, c.Distance, c.EntryFee);
}
