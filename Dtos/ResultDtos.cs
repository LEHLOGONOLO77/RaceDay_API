namespace RaceDay.Api;

public class CreateResultDto
{
    public int ParticipantId { get; set; }
    public int EventId { get; set; }
    public int EventCategoryId { get; set; }
    [Range(1, 100000)] public int Position { get; set; }
    /// <summary>Finish time as hh:mm:ss, e.g. 00:45:30.</summary>
    public TimeSpan FinishTime { get; set; }
}

public class UpdateResultDto
{
    [Range(1, 100000)] public int Position { get; set; }
    /// <summary>Finish time as hh:mm:ss, e.g. 00:45:30.</summary>
    public TimeSpan FinishTime { get; set; }
}

public record ResultResponse(int Id, int ParticipantId, string ParticipantName, int EventId, string EventName,
    int EventCategoryId, string CategoryName, int Position, TimeSpan FinishTime)
{
    public static ResultResponse From(Result r) => new(r.Id, r.ParticipantId,
        $"{r.Participant.Name} {r.Participant.Surname}", r.EventId, r.Event.Name,
        r.EventCategoryId, r.EventCategory.CategoryName, r.Position, r.FinishTime);
}
