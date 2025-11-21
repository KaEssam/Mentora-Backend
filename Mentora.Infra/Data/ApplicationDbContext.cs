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
    public DbSet<SessionTemplate> SessionTemplates { get; set; }
    public DbSet<FileEntity> Files { get; set; }
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<ReminderSettings> ReminderSettings { get; set; }
    public DbSet<SessionFeedback> SessionFeedbacks { get; set; }
    public DbSet<FeedbackRating> FeedbackRatings { get; set; }

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
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.MentorId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("USD");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.MeetingUrl).HasMaxLength(500);
            entity.Property(e => e.CancelReason).HasMaxLength(500);
            entity.Property(e => e.PaymentIntentId).HasMaxLength(200);

            // Note: SessionId is stored as string for flexibility, no direct navigation to Session entity
            // This avoids EF Core relationship mapping issues between string SessionId and int Session.Id

            // Configure navigation to ApplicationUser (Mentor)
            entity.HasOne<ApplicationUser>()
                  .WithMany(e => e.MentorBookings)
                  .HasForeignKey(e => e.MentorId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("FK_Booking_Mentor_ApplicationUser");

            // Configure navigation to ApplicationUser (User/Mentee)
            entity.HasOne<ApplicationUser>()
                  .WithMany(e => e.MenteeBookings)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("FK_Booking_User_ApplicationUser");

            // Indexes for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.MentorId);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.MentorId, e.SessionStartTime, e.Status });
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });

            // TODO: INTEGRATION - Payment Integration - Add payment status and transaction ID fields when payment system is implemented
        });

        // Configure SessionTemplate entity
        builder.Entity<SessionTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.MentorId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.BasePrice).HasPrecision(18, 2);
            entity.Property(e => e.DefaultNotes).HasMaxLength(2000);
            entity.Property(e => e.DefaultRecurrenceJson).HasMaxLength(2000);

            // Configure navigation to ApplicationUser
            entity.HasOne<ApplicationUser>()
                  .WithMany(e => e.CreatedSessionTemplates)
                  .HasForeignKey(e => e.MentorId)
                  .OnDelete(DeleteBehavior.Cascade);
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

        // Configure Reminder entity
        builder.Entity<Reminder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Subject).HasMaxLength(500);
            entity.Property(e => e.Message).HasMaxLength(2000);
            entity.Property(e => e.RecipientEmail).HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);

            // Configure navigation to ApplicationUser
            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Note: SessionId navigation removed to avoid type mismatch between string SessionId and int Session.Id
            // This maintains data integrity while preventing EF Core relationship mapping issues

            // Indexes for performance
            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.HasIndex(e => new { e.ScheduledAt, e.Status });
            entity.HasIndex(e => new { e.SessionId, e.Type });
        });

        // Configure ReminderSettings entity
        builder.Entity<ReminderSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.UserTimeZone).HasMaxLength(50);

            // Configure navigation to ApplicationUser
            entity.HasOne<ApplicationUser>()
                  .WithOne()
                  .HasForeignKey<ReminderSettings>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Ensure unique user settings
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // Configure SessionFeedback entity
        builder.Entity<SessionFeedback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.MentorId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.WhatWentWell).HasMaxLength(2000);
            entity.Property(e => e.WhatCouldBeImproved).HasMaxLength(2000);
            entity.Property(e => e.AdditionalComments).HasMaxLength(2000);
            entity.Property(e => e.MentorResponse).HasMaxLength(2000);
            entity.Property(e => e.FlaggedReason).HasMaxLength(500);

            // Configure navigation to ApplicationUser
            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Note: SessionId navigation removed to avoid type mismatch between string SessionId and int Session.Id
            // This maintains data integrity while preventing EF Core relationship mapping issues

            // Indexes for performance
            entity.HasIndex(e => new { e.SessionId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.MentorId);
            entity.HasIndex(e => e.OverallRating);
            entity.HasIndex(e => e.IsPublic);
            entity.HasIndex(e => e.IsVerified);
            entity.HasIndex(e => e.IsFlagged);
            entity.HasIndex(e => e.IsHidden);
        });

        // Configure FeedbackRating entity
        builder.Entity<FeedbackRating>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CriteriaName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Comment).HasMaxLength(500);

            // Configure navigation to SessionFeedback
            entity.HasOne(fr => fr.Feedback)
                  .WithMany(f => f.Ratings)
                  .HasForeignKey(fr => fr.FeedbackId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            entity.HasIndex(e => e.FeedbackId);
            entity.HasIndex(e => e.CriteriaName);
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
