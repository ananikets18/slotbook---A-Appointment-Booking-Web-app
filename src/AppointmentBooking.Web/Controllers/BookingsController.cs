using System.Security.Claims;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Services;
using AppointmentBooking.Core.Enums;
using AppointmentBooking.Core.Exceptions;
using AppointmentBooking.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Web.Controllers;

[Authorize]
public class BookingsController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(IBookingService bookingService, ILogger<BookingsController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? status)
    {
        var userId = GetCurrentUserId();
        var bookings = await _bookingService.GetUserBookingsAsync(userId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BookingStatus>(status, true, out var filterStatus))
        {
            bookings = bookings.Where(b => b.Status == filterStatus).ToList();
        }

        var viewModels = bookings.Select(b => new BookingItemViewModel
        {
            Id = b.Id,
            CustomerName = b.CustomerName,
            CustomerEmail = b.CustomerEmail,
            CustomerPhone = b.CustomerPhone,
            ProviderName = b.ProviderName,
            ServiceName = b.ServiceName,
            TotalPrice = b.TotalPrice,
            SlotDate = b.SlotDate,
            StartTime = b.StartTime,
            EndTime = b.EndTime,
            Status = b.Status,
            CustomerNotes = b.CustomerNotes,
            CancellationReason = b.CancellationReason,
            CreatedAt = b.CreatedAt
        }).ToList();

        var model = new BookingListViewModel
        {
            Bookings = viewModels,
            StatusFilter = status
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var booking = await _bookingService.GetByIdAsync(id);
        if (booking is null)
        {
            return NotFound("Booking not found.");
        }

        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        // Customer can only view their own booking
        if (userRole == UserRole.Customer && booking.UserId != userId)
        {
            return Forbid();
        }

        var item = new BookingItemViewModel
        {
            Id = booking.Id,
            CustomerName = booking.CustomerName,
            CustomerEmail = booking.CustomerEmail,
            CustomerPhone = booking.CustomerPhone,
            ProviderName = booking.ProviderName,
            ServiceName = booking.ServiceName,
            TotalPrice = booking.TotalPrice,
            SlotDate = booking.SlotDate,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            Status = booking.Status,
            CustomerNotes = booking.CustomerNotes,
            CancellationReason = booking.CancellationReason,
            CreatedAt = booking.CreatedAt
        };

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] Guid serviceId, [FromForm] Guid slotId, [FromForm] string? customerNotes)
    {
        var userId = GetCurrentUserId();

        try
        {
            var request = new CreateBookingRequest(serviceId, slotId, customerNotes);
            var booking = await _bookingService.CreateBookingAsync(userId, request);

            TempData["SuccessMessage"] = $"Your appointment with {booking.ProviderName} has been booked successfully! (Status: {booking.Status})";
            return RedirectToAction("Index");
        }
        catch (ConflictException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Index", "Providers");
        }
        catch (ValidationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Index", "Providers");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, [FromForm] string? reason)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        try
        {
            var request = new UpdateBookingStatusRequest(BookingStatus.Cancelled, reason ?? "Cancelled by user");
            await _bookingService.UpdateStatusAsync(id, userId, userRole, request);

            TempData["SuccessMessage"] = "Appointment has been cancelled and the slot has been freed.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction("Index");
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private UserRole GetCurrentUserRole()
    {
        var claim = User.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse<UserRole>(claim, true, out var role) ? role : UserRole.Customer;
    }
}
