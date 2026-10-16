namespace RaceDay.Api;

public enum UserRole { Organiser = 1, Participant = 2 }
public enum EventType { Run = 1, Walk = 2, Cycle = 3 }
public enum EventStatus { Draft = 1, Open = 2, Closed = 3, Completed = 4, Cancelled = 5 }
public enum PaymentStatus { Pending = 1, Paid = 2, Refunded = 3 }
public enum RegistrationStatus { Pending = 1, Confirmed = 2, Cancelled = 3 }
