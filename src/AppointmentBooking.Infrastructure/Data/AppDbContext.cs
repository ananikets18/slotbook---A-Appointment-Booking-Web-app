using AppointmentBooking.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Slot> Slots => Set<Slot>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- User Configuration ---
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);
            b.Property(u => u.Email).HasMaxLength(150).IsRequired();
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            b.Property(u => u.PhoneNumber).HasMaxLength(30);
            b.Property(u => u.PasswordHash).IsRequired();
            b.Property(u => u.Role).HasConversion<int>().IsRequired();
        });

        // --- Provider Configuration ---
        modelBuilder.Entity<Provider>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.BusinessName).HasMaxLength(150).IsRequired();
            b.Property(p => p.Category).HasMaxLength(50).IsRequired();
            b.Property(p => p.Description).HasMaxLength(1000);
            b.Property(p => p.Address).HasMaxLength(300);
            b.Property(p => p.ContactPhone).HasMaxLength(30);

            b.HasOne(p => p.User)
             .WithOne(u => u.ProviderProfile)
             .HasForeignKey<Provider>(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Service Configuration ---
        modelBuilder.Entity<Service>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Name).HasMaxLength(150).IsRequired();
            b.Property(s => s.Description).HasMaxLength(1000);
            b.Property(s => s.Price).HasPrecision(10, 2).IsRequired();
            b.Property(s => s.DurationMinutes).HasDefaultValue(30);

            b.HasOne(s => s.Provider)
             .WithMany(p => p.Services)
             .HasForeignKey(s => s.ProviderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Slot Configuration ---
        modelBuilder.Entity<Slot>(b =>
        {
            b.HasKey(s => s.Id);

            // Composite unique index: A provider cannot have two identical slots on same date/time
            b.HasIndex(s => new { s.ProviderId, s.Date, s.StartTime })
             .IsUnique();

            // Optimistic concurrency token
            b.Property(s => s.ConcurrencyStamp)
             .IsConcurrencyToken()
             .IsRequired();

            b.HasOne(s => s.Provider)
             .WithMany(p => p.Slots)
             .HasForeignKey(s => s.ProviderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Booking Configuration ---
        modelBuilder.Entity<Booking>(b =>
        {
            b.HasKey(b => b.Id);

            // STANDOUT REQUIREMENT: Unique constraint on SlotId!
            // Ensures double-booking is strictly impossible at the DB schema level.
            b.HasIndex(b => b.SlotId)
             .IsUnique();

            b.Property(b => b.TotalPrice).HasPrecision(10, 2);
            b.Property(b => b.Status).HasConversion<int>().IsRequired();
            b.Property(b => b.CustomerNotes).HasMaxLength(1000);
            b.Property(b => b.CancellationReason).HasMaxLength(500);

            b.HasOne(b => b.User)
             .WithMany(u => u.Bookings)
             .HasForeignKey(b => b.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(b => b.Service)
             .WithMany(s => s.Bookings)
             .HasForeignKey(b => b.ServiceId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(b => b.Slot)
             .WithOne(s => s.Booking)
             .HasForeignKey<Booking>(b => b.SlotId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
