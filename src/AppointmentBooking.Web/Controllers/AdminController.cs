using AppointmentBooking.Application.Services;
using AppointmentBooking.Core.Enums;
using AppointmentBooking.Core.Interfaces;
using AppointmentBooking.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IUserRepository _userRepo;
    private readonly IProviderService _providerService;
    private readonly IBookingService _bookingService;

    public AdminController(
        IUserRepository userRepo,
        IProviderService providerService,
        IBookingService bookingService)
    {
        _userRepo = userRepo;
        _providerService = providerService;
        _bookingService = bookingService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userRepo.GetAllAsync();
        var providers = await _providerService.GetAllProvidersForAdminAsync();
        var bookings = await _bookingService.GetAllBookingsAsync();

        var bookingItems = bookings.Select(b => new BookingItemViewModel
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

        var model = new AdminDashboardViewModel
        {
            TotalUsers = users.Count,
            TotalProviders = providers.Count,
            TotalBookings = bookings.Count,
            ConfirmedBookings = bookings.Count(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed),
            TotalPlatformVolume = bookings.Where(b => b.Status == BookingStatus.Completed).Sum(b => b.TotalPrice),
            Providers = providers,
            RecentBookings = bookingItems.Take(15).ToList()
        };

        return View(model);
    }
}
