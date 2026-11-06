using System.Security.Claims;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Services;
using AppointmentBooking.Core.Enums;
using AppointmentBooking.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Web.Controllers;

[Authorize(Roles = "Provider")]
public class ProviderDashboardController : Controller
{
    private readonly IProviderService _providerService;
    private readonly IBookingService _bookingService;
    private readonly ISlotService _slotService;
    private readonly ILogger<ProviderDashboardController> _logger;

    public ProviderDashboardController(
        IProviderService providerService,
        IBookingService bookingService,
        ISlotService slotService,
        ILogger<ProviderDashboardController> logger)
    {
        _providerService = providerService;
        _bookingService = bookingService;
        _slotService = slotService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var provider = await GetCurrentProviderAsync();
        if (provider is null)
        {
            return RedirectToAction("ProfileSetup");
        }

        var bookings = await _bookingService.GetProviderBookingsAsync(provider.Id);
        var services = await _providerService.GetServicesByProviderIdAsync(provider.Id, activeOnly: false);

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

        var model = new ProviderDashboardViewModel
        {
            Provider = provider,
            RecentBookings = bookingItems,
            PendingCount = bookingItems.Count(b => b.Status == BookingStatus.Pending),
            ConfirmedCount = bookingItems.Count(b => b.Status == BookingStatus.Confirmed),
            CompletedCount = bookingItems.Count(b => b.Status == BookingStatus.Completed),
            TotalServicesCount = services.Count,
            TotalRevenue = bookingItems.Where(b => b.Status == BookingStatus.Completed).Sum(b => b.TotalPrice)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid bookingId, BookingStatus newStatus, string? reason)
    {
        var userId = GetCurrentUserId();

        try
        {
            var request = new UpdateBookingStatusRequest(newStatus, reason);
            await _bookingService.UpdateStatusAsync(bookingId, userId, UserRole.Provider, request);
            TempData["SuccessMessage"] = $"Appointment status updated to {newStatus}.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Services()
    {
        var provider = await GetCurrentProviderAsync();
        if (provider is null) return RedirectToAction("ProfileSetup");

        var services = await _providerService.GetServicesByProviderIdAsync(provider.Id, activeOnly: false);

        var model = new ManageServicesViewModel
        {
            ProviderId = provider.Id,
            Services = services
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddService(CreateServiceRequest request)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider is null) return RedirectToAction("ProfileSetup");

        if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0)
        {
            TempData["ErrorMessage"] = "Please provide a valid service name and positive price.";
            return RedirectToAction("Services");
        }

        await _providerService.AddServiceAsync(provider.Id, request);
        TempData["SuccessMessage"] = $"Service '{request.Name}' was added successfully.";
        return RedirectToAction("Services");
    }

    [HttpGet]
    public async Task<IActionResult> Slots([FromQuery] string? date)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider is null) return RedirectToAction("ProfileSetup");

        var selectedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var parsedDate))
        {
            selectedDate = parsedDate;
        }

        var slots = await _slotService.GetAllSlotsForDateAsync(provider.Id, selectedDate);

        var model = new ManageSlotsViewModel
        {
            ProviderId = provider.Id,
            SelectedDate = selectedDate,
            Slots = slots,
            GenerateFromDate = selectedDate,
            GenerateToDate = selectedDate.AddDays(7)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateSlots([FromForm] DateOnly fromDate, [FromForm] DateOnly toDate)
    {
        var provider = await GetCurrentProviderAsync();
        if (provider is null) return RedirectToAction("ProfileSetup");

        try
        {
            var count = await _slotService.GenerateSlotsAsync(provider.Id, fromDate, toDate);
            TempData["SuccessMessage"] = $"Successfully generated {count} slots (30-min each, 09:00 - 17:00) between {fromDate:dd MMM} and {toDate:dd MMM}.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction("Slots", new { date = fromDate.ToString("yyyy-MM-dd") });
    }

    [HttpGet]
    public IActionResult ProfileSetup()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProfileSetup(CreateProviderRequest request)
    {
        var userId = GetCurrentUserId();
        try
        {
            await _providerService.CreateProviderProfileAsync(userId, request);
            TempData["SuccessMessage"] = "Provider business profile created successfully!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return View();
        }
    }

    private async Task<ProviderDto?> GetCurrentProviderAsync()
    {
        var userId = GetCurrentUserId();
        return await _providerService.GetByUserIdAsync(userId);
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
