using AppointmentBooking.Core.Entities;
using AppointmentBooking.Core.Enums;
using AppointmentBooking.Core.Exceptions;

namespace AppointmentBooking.Application.Services;

public static class BookingStatusService
{
    public static bool CanTransition(
        BookingStatus currentStatus,
        BookingStatus targetStatus,
        UserRole userRole,
        DateOnly slotDate,
        TimeOnly slotStartTime,
        DateTime nowUtc)
    {
        // Terminal states cannot transition
        if (currentStatus is BookingStatus.Cancelled or BookingStatus.Completed)
        {
            return false;
        }

        // Cannot transition to the same status
        if (currentStatus == targetStatus)
        {
            return false;
        }

        return (currentStatus, targetStatus) switch
        {
            // Pending -> Confirmed: only Provider or Admin
            (BookingStatus.Pending, BookingStatus.Confirmed) =>
                userRole is UserRole.Provider or UserRole.Admin,

            // Pending -> Cancelled: any role
            (BookingStatus.Pending, BookingStatus.Cancelled) => true,

            // Confirmed -> Completed: only Provider or Admin (typically at or after appointment)
            (BookingStatus.Confirmed, BookingStatus.Completed) =>
                userRole is UserRole.Provider or UserRole.Admin,

            // Confirmed -> Cancelled:
            // Admin and Provider can always cancel; Customer can only cancel before start time
            (BookingStatus.Confirmed, BookingStatus.Cancelled) =>
                userRole is UserRole.Admin or UserRole.Provider ||
                (userRole is UserRole.Customer && IsFutureSlot(slotDate, slotStartTime, nowUtc)),

            _ => false
        };
    }

    public static void ValidateTransition(
        BookingStatus currentStatus,
        BookingStatus targetStatus,
        UserRole userRole,
        DateOnly slotDate,
        TimeOnly slotStartTime,
        DateTime nowUtc)
    {
        if (currentStatus is BookingStatus.Cancelled or BookingStatus.Completed)
        {
            throw new ConflictException($"Cannot change status of a {currentStatus} booking.");
        }

        if (currentStatus == targetStatus)
        {
            throw new ConflictException($"Booking is already in {targetStatus} status.");
        }

        if (!CanTransition(currentStatus, targetStatus, userRole, slotDate, slotStartTime, nowUtc))
        {
            if (userRole == UserRole.Customer && currentStatus == BookingStatus.Confirmed && targetStatus == BookingStatus.Cancelled)
            {
                throw new ForbiddenException("Customers cannot cancel an appointment that has already started or passed.");
            }

            throw new ForbiddenException($"User with role '{userRole}' is not authorized to transition booking from '{currentStatus}' to '{targetStatus}'.");
        }
    }

    private static bool IsFutureSlot(DateOnly date, TimeOnly time, DateTime nowUtc)
    {
        var slotDateTime = date.ToDateTime(time, DateTimeKind.Utc);
        return slotDateTime > nowUtc;
    }
}
