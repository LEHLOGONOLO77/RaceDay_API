using Microsoft.EntityFrameworkCore;

namespace RaceDay.Api;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventRoute> Routes => Set<EventRoute>();
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();

        b.Entity<User>()
            .HasOne(u => u.Organiser)
            .WithOne(o => o.User)
            .HasForeignKey<Organiser>(o => o.UserId);

        b.Entity<User>()
            .HasOne(u => u.Participant)
            .WithOne(p => p.User)
            .HasForeignKey<Participant>(p => p.UserId);

        b.Entity<Event>()
            .HasOne(e => e.Organiser)
            .WithMany(o => o.Events)
            .HasForeignKey(e => e.OrganiserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Event>()
            .Property(e => e.Distance)
            .HasPrecision(6, 2);

        b.Entity<EventRoute>()
            .Property(r => r.Distance)
            .HasPrecision(6, 2);

        b.Entity<EventCategory>()
            .Property(c => c.Distance)
            .HasPrecision(6, 2);

        b.Entity<EventCategory>()
            .Property(c => c.EntryFee)
            .HasPrecision(10, 2);

        b.Entity<EventRoute>()
            .HasOne(r => r.Event)
            .WithMany(e => e.Routes)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<EventCategory>()
            .HasOne(c => c.Event)
            .WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Registration>()
            .HasOne(r => r.Participant)
            .WithMany(p => p.Registrations)
            .HasForeignKey(r => r.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Registration>()
            .HasOne(r => r.EventCategory)
            .WithMany(c => c.Registrations)
            .HasForeignKey(r => r.EventCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Registration>()
            .HasIndex(r => new { r.ParticipantId, r.EventCategoryId })
            .IsUnique();

        b.Entity<Result>()
            .HasOne(r => r.Participant)
            .WithMany()
            .HasForeignKey(r => r.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Result>()
            .HasOne(r => r.Event)
            .WithMany()
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Result>()
            .HasOne(r => r.EventCategory)
            .WithMany()
            .HasForeignKey(r => r.EventCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Result>()
            .HasIndex(r => new { r.ParticipantId, r.EventId })
            .IsUnique();
    }
}
