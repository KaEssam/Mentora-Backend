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

        // Configure Session entity
        builder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.MentorId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Price).HasPrecision(18, 2);

            // Configure navigation property to ApplicationUser
            entity.HasOne<ApplicationUser>()
                  .WithMany(e => e.MentorSessions)
                  .HasForeignKey(e => e.MentorId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Configure collection navigation for bookings
            entity.HasMany<Booking>()
                  .WithOne()
                  .HasForeignKey(e => e.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Ensure EndAt is after StartAt
            entity.ToTable(t => t.HasCheckConstraint("CK_Session_EndAt_After_StartAt", "EndAt > StartAt"));
            // TODO: INTEGRATION - Session Enhancement - Add session recurrence pattern configuration when session scheduling features are implemented
        });

        // Configure Booking entity
        builder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.SessionId).IsRequired();
            entity.Property(e => e.MentorId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.MenteeId).IsRequired().HasMaxLength(450);

            // Configure navigation to Session
            entity.HasOne<Session>()
                  .WithMany()
                  .HasForeignKey(e => e.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Configure navigation to ApplicationUser (Mentor)
            entity.HasOne<ApplicationUser>()
                  .WithMany(e => e.MentorBookings)
                  .HasForeignKey(e => e.MentorId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("FK_Booking_Mentor_ApplicationUser");

            // Configure navigation to ApplicationUser (Mentee)
            entity.HasOne<ApplicationUser>()
                  .WithMany(e => e.MenteeBookings)
                  .HasForeignKey(e => e.MenteeId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("FK_Booking_Mentee_ApplicationUser");
            // TODO: INTEGRATION - Payment Integration - Add payment status and transaction ID fields when payment system is implemented
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

            // Configure navigation to ApplicationUser
            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(e => e.UploadedById)
                  .OnDelete(DeleteBehavior.Cascade);
            // TODO: INTEGRATION - File Processing - Add file processing status and metadata when file processing features are implemented
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
