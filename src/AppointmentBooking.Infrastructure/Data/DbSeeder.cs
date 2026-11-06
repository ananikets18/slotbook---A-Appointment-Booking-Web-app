using AppointmentBooking.Core.DomainServices;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppointmentBooking.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Users.AnyAsync())
        {
            logger.LogInformation("Database already contains data. Skipping seeding.");
            return;
        }

        logger.LogInformation("Seeding initial demonstration data...");

        // Passwords hashed with BCrypt
        var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
        var providerPasswordHash = BCrypt.Net.BCrypt.HashPassword("Provider@123");
        var customerPasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123");

        // 1. Admin
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@slotbook.in",
            FullName = "Rajesh Sharma (Admin)",
            PhoneNumber = "+91 98200 11000",
            PasswordHash = adminPasswordHash,
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(admin);

        // 2. Providers
        var doctorUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "clinic@slotbook.in",
            FullName = "Dr. Ananya Deshmukh, MBBS, MD",
            PhoneNumber = "+91 98201 22334",
            PasswordHash = providerPasswordHash,
            Role = UserRole.Provider,
            CreatedAt = DateTime.UtcNow
        };
        var clinicProvider = new Provider
        {
            Id = Guid.NewGuid(),
            UserId = doctorUser.Id,
            BusinessName = "Arogya Family Healthcare & Diagnostic Clinic",
            Category = "Clinic",
            Description = "Trusted multi-speciality primary care clinic offering general physician consultations, preventive health screenings, diabetic management, and pediatric checkups.",
            Address = "Shop 12, Ground Floor, Indiranagar 100ft Road, Bengaluru, Karnataka 560038",
            ContactPhone = "+91 98201 22334",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(doctorUser);
        context.Providers.Add(clinicProvider);

        var clinicServices = new List<Service>
        {
            new() { Id = Guid.NewGuid(), ProviderId = clinicProvider.Id, Name = "General Physician Consultation", Description = "Routine diagnosis, blood pressure examination, and symptom evaluation.", Price = 500.00m, DurationMinutes = 30, IsActive = true },
            new() { Id = Guid.NewGuid(), ProviderId = clinicProvider.Id, Name = "Full Body Preventive Health Checkup", Description = "Comprehensive physical checkup, BMI analysis, vitals assessment, and wellness guidance.", Price = 1500.00m, DurationMinutes = 30, IsActive = true },
            new() { Id = Guid.NewGuid(), ProviderId = clinicProvider.Id, Name = "Diabetes & Hypertension Review", Description = "Blood glucose tracking review, diet consultation, and cardiovascular risk screening.", Price = 800.00m, DurationMinutes = 30, IsActive = true }
        };
        context.Services.AddRange(clinicServices);

        var salonUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "salon@slotbook.in",
            FullName = "Rohan Malhotra",
            PhoneNumber = "+91 98191 33445",
            PasswordHash = providerPasswordHash,
            Role = UserRole.Provider,
            CreatedAt = DateTime.UtcNow
        };
        var salonProvider = new Provider
        {
            Id = Guid.NewGuid(),
            UserId = salonUser.Id,
            BusinessName = "Kaya Touch Luxury Salon & Spa",
            Category = "Salon",
            Description = "Modern boutique salon and unisex grooming lounge specializing in precision haircuts, beard styling, organic skin treatments, and bridal makeovers.",
            Address = "Plot 45, Linking Road, Near Bandra Station, Bandra West, Mumbai, Maharashtra 400050",
            ContactPhone = "+91 98191 33445",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(salonUser);
        context.Providers.Add(salonProvider);

        var salonServices = new List<Service>
        {
            new() { Id = Guid.NewGuid(), ProviderId = salonProvider.Id, Name = "Signature Haircut & Beard Styling", Description = "Tailored styling by senior stylist, relaxing hair wash, beard shaping, and blow-dry.", Price = 600.00m, DurationMinutes = 30, IsActive = true },
            new() { Id = Guid.NewGuid(), ProviderId = salonProvider.Id, Name = "Detoxifying Charcoal Facial & Massage", Description = "Deep pore cleansing, herbal steam exfoliation, facial acupressure, and cooling mask.", Price = 1200.00m, DurationMinutes = 30, IsActive = true },
            new() { Id = Guid.NewGuid(), ProviderId = salonProvider.Id, Name = "Ayurvedic Hair Spa & Scalp Therapy", Description = "Warm herbal oil massage, deep nourishing hair mask, and ozone steam therapy.", Price = 950.00m, DurationMinutes = 30, IsActive = true }
        };
        context.Services.AddRange(salonServices);

        var tutorUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "tutor@slotbook.in",
            FullName = "Prof. Sneha Kulkarni, M.Tech (IIT Bombay)",
            PhoneNumber = "+91 98332 44556",
            PasswordHash = providerPasswordHash,
            Role = UserRole.Provider,
            CreatedAt = DateTime.UtcNow
        };
        var tutorProvider = new Provider
        {
            Id = Guid.NewGuid(),
            UserId = tutorUser.Id,
            BusinessName = "Vidya STEM & Coding Academy",
            Category = "Tutor",
            Description = "Premier academic and tech tutoring academy led by IIT alumni. Mentoring students in IIT-JEE Mathematics, Physics, and Software Engineering (.NET, DSA, System Design).",
            Address = "Lane 5, Koregaon Park, Near North Main Road, Pune, Maharashtra 411001",
            ContactPhone = "+91 98332 44556",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(tutorUser);
        context.Providers.Add(tutorProvider);

        var tutorServices = new List<Service>
        {
            new() { Id = Guid.NewGuid(), ProviderId = tutorProvider.Id, Name = "IIT-JEE Advanced Mathematics 1-on-1", Description = "Calculus, coordinate geometry, trigonometry, past year problem breakdown, and test strategies.", Price = 1200.00m, DurationMinutes = 30, IsActive = true },
            new() { Id = Guid.NewGuid(), ProviderId = tutorProvider.Id, Name = "Data Structures, Algorithms & C# Mentorship", Description = "LeetCode problem walkthroughs, C# OOP concepts, LINQ patterns, and technical interview prep.", Price = 1500.00m, DurationMinutes = 30, IsActive = true },
            new() { Id = Guid.NewGuid(), ProviderId = tutorProvider.Id, Name = "Class 11 & 12 Physics Problem Solving", Description = "Mechanics, electromagnetism, numerical practice, and board exam revision.", Price = 1000.00m, DurationMinutes = 30, IsActive = true }
        };
        context.Services.AddRange(tutorServices);

        // 3. Customers
        var customer1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "customer@slotbook.in",
            FullName = "Aarav Mehta",
            PhoneNumber = "+91 98450 12345",
            PasswordHash = customerPasswordHash,
            Role = UserRole.Customer,
            CreatedAt = DateTime.UtcNow
        };
        var customer2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "pooja@slotbook.in",
            FullName = "Pooja Sundaram",
            PhoneNumber = "+91 98450 67890",
            PasswordHash = customerPasswordHash,
            Role = UserRole.Customer,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.AddRange(customer1, customer2);

        // 4. Slots generation for the next 14 days
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = today.AddDays(14);

        var allSlots = new List<Slot>();
        allSlots.AddRange(SlotGenerator.GenerateFixedDailySlots(clinicProvider.Id, today, endDate));
        allSlots.AddRange(SlotGenerator.GenerateFixedDailySlots(salonProvider.Id, today, endDate));
        allSlots.AddRange(SlotGenerator.GenerateFixedDailySlots(tutorProvider.Id, today, endDate));
        context.Slots.AddRange(allSlots);

        // 5. Seed sample bookings on tomorrow's slots
        var tomorrow = today.AddDays(1);

        // Clinic booking for customer1
        var clinicSlot = allSlots.First(s => s.ProviderId == clinicProvider.Id && s.Date == tomorrow && s.StartTime == new TimeOnly(10, 0));
        clinicSlot.IsBooked = true;
        clinicSlot.ConcurrencyStamp = Guid.NewGuid();

        var booking1 = new Booking
        {
            Id = Guid.NewGuid(),
            UserId = customer1.Id,
            ServiceId = clinicServices[0].Id,
            SlotId = clinicSlot.Id,
            Status = BookingStatus.Confirmed,
            TotalPrice = clinicServices[0].Price,
            CustomerNotes = "Quarterly routine health review and BP check.",
            CreatedAt = DateTime.UtcNow.AddHours(-3)
        };
        context.Bookings.Add(booking1);

        // Salon booking for customer2
        var salonSlot = allSlots.First(s => s.ProviderId == salonProvider.Id && s.Date == tomorrow && s.StartTime == new TimeOnly(14, 0));
        salonSlot.IsBooked = true;
        salonSlot.ConcurrencyStamp = Guid.NewGuid();

        var booking2 = new Booking
        {
            Id = Guid.NewGuid(),
            UserId = customer2.Id,
            ServiceId = salonServices[0].Id,
            SlotId = salonSlot.Id,
            Status = BookingStatus.Pending,
            TotalPrice = salonServices[0].Price,
            CustomerNotes = "Need haircut and facial styling before family wedding function.",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        context.Bookings.Add(booking2);

        await context.SaveChangesAsync();
        logger.LogInformation("Database seeded successfully with {SlotCount} slots, 3 providers, 2 customers, 1 admin, and sample bookings.", allSlots.Count);
    }
}
