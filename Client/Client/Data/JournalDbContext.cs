using Client.Models;
using Microsoft.EntityFrameworkCore;

namespace Client.Data;

public class JournalDbContext : DbContext
{
    public JournalDbContext(DbContextOptions<JournalDbContext> options)
        : base(options)
    {
    }

    public DbSet<JournalEventType> EventTypes => Set<JournalEventType>();
    public DbSet<JournalEntry> Entries => Set<JournalEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<JournalEventType>(entity =>
        {
            entity.ToTable("journal_event_types");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(32).IsRequired();
            entity.Property(x => x.DefaultSeverity).HasMaxLength(16).IsRequired();
            entity.Property(x => x.RequiresComment).HasDefaultValue(false);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.SortOrder).HasDefaultValue(0);
        });

        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.ToTable("journal_entries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Timestamp).IsRequired();
            entity.Property(x => x.Level).HasMaxLength(16).IsRequired();
            entity.Property(x => x.Source).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Screen).HasMaxLength(64);
            entity.Property(x => x.EntityType).HasMaxLength(64);
            entity.Property(x => x.UsernameSnapshot).HasMaxLength(64);
            entity.Property(x => x.DetailsJson).HasColumnType("longtext");
            entity.Property(x => x.Message).HasColumnType("longtext").IsRequired();
            entity.Property(x => x.Comment).HasColumnType("longtext");
            entity.Property(x => x.IsResolved).HasDefaultValue(false);

            entity.HasIndex(x => x.Timestamp);
            entity.HasIndex(x => x.Level);
            entity.HasIndex(x => x.Source);
            entity.HasIndex(x => x.IsResolved);
            entity.HasIndex(x => x.EventTypeId);

            entity.HasOne(x => x.EventType)
                .WithMany(x => x.Entries)
                .HasForeignKey(x => x.EventTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
