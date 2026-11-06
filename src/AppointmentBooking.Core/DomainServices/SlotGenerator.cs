using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Exceptions;

namespace AppointmentBooking.Core.DomainServices;

public static class SlotGenerator
{
    public const int StartHour = 9;
    public const int EndHour = 17;
    public const int SlotDurationMinutes = 30;
    public const int SlotsPerDay = 16;

    public static List<Slot> GenerateFixedDailySlots(Guid providerId, DateOnly fromDate, DateOnly toDate)
    {
        if (toDate < fromDate)
        {
            throw new ValidationException("End date cannot be earlier than start date.");
        }

        var totalDays = toDate.DayNumber - fromDate.DayNumber + 1;
        if (totalDays > 60)
        {
            throw new ValidationException("Cannot generate slots for more than 60 days at a time.");
        }

        var slots = new List<Slot>();

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            for (var hour = StartHour; hour < EndHour; hour++)
            {
                for (var minute = 0; minute < 60; minute += SlotDurationMinutes)
                {
                    var start = new TimeOnly(hour, minute);
                    var end = start.AddMinutes(SlotDurationMinutes);

                    slots.Add(new Slot
                    {
                        Id = Guid.NewGuid(),
                        ProviderId = providerId,
                        Date = date,
                        StartTime = start,
                        EndTime = end,
                        IsBooked = false,
                        ConcurrencyStamp = Guid.NewGuid()
                    });
                }
            }
        }

        return slots;
    }
}
