using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Mentora.Core.Data;
using FileEntity = Mentora.Core.Data.File;

namespace Mentora.Infra.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // User DbSet is provided by IdentityDbContext<ApplicationUser>
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<FileEntity> Files { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicationUser is configured by Identity automatically

        // Configure User entity to ignore navigation properties that conflict
        builder.Entity<User>(entity =>
        {
            entity.Ignore(e => e.MentorSessions);
            entity.Ignore(e => e.MentorBookings);
            entity.Ignore(e => e.MenteeBookings);
        });

        // Configure Session entity
        builder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.MentorId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Price).HasPrecision(18, 2);

            entity.HasOne<ApplicationUser>()
                  .WithMany(e => e.MentorSessions)
                  .HasForeignKey(e => e.MentorId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Ignore the navigation property to User since it conflicts with ApplicationUser mapping
            entity.Ignore(e => e.Mentor);

            // Ensure EndAt is after StartAt
            entity.ToTable(t => t.HasCheckConstraint("CK_Session_EndAt_After_StartAt", "EndAt > StartAt"));
        });

        // Configure Booking entity
        builder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.MentorId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.MenteeId).IsRequired().HasMaxLength(450);

            entity.HasOne(e => e.Session)
                  .WithMany(e => e.Bookings)
                  .HasForeignKey(e => e.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Configure explicit relationships with ApplicationUser to avoid shadow properties
            entity.HasOne<ApplicationUser>()
                  .WithMany(e => e.MentorBookings)
                  .HasForeignKey(e => e.MentorId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("FK_Booking_Mentor_ApplicationUser");

            entity.HasOne<ApplicationUser>()
                  .WithMany(e => e.MenteeBookings)
                  .HasForeignKey(e => e.MenteeId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("FK_Booking_Mentee_ApplicationUser");

            // Ignore the navigation properties to User since they conflict with ApplicationUser mappings
            entity.Ignore(e => e.Mentor);
            entity.Ignore(e => e.Mentee);
        });

        // Configure File entity
        builder.Entity<FileEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UploadedById).IsRequired().HasMaxLength(450);

            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(e => e.UploadedById)
                  .OnDelete(DeleteBehavior.Cascade);

            // Ignore the navigation property to User since it conflicts with ApplicationUser mapping
            entity.Ignore(e => e.UploadedBy);
        });

        // Seed initial data
        SeedData(builder);
    }

    private void SeedData(ModelBuilder builder)
    {
        // Can add seed data here if needed
        // For example: default roles, admin user, etc.
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update UpdatedAt timestamp for modified entities
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is ApplicationUser && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                ((ApplicationUser)entry.Entity).CreatedAt = DateTime.UtcNow;
            }
            ((ApplicationUser)entry.Entity).UpdatedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}