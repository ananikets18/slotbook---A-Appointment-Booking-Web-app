using AppointmentBooking.Application.Services;
using AppointmentBooking.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Web.Controllers;

public class ProvidersController : Controller
{
    private readonly IProviderService _providerService;
    private readonly ISlotService _slotService;

    public ProvidersController(IProviderService providerService, ISlotService slotService)
    {
        _providerService = providerService;
        _slotService = slotService;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? category, [FromQuery] string? search)
    {
        var providers = await _providerService.GetProvidersAsync(category);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            providers = providers.Where(p =>
                p.BusinessName.ToLowerInvariant().Contains(term) ||
                (p.Description?.ToLowerInvariant().Contains(term) ?? false) ||
                p.Category.ToLowerInvariant().Contains(term)).ToList();
        }

        var model = new ProviderListViewModel
        {
            Providers = providers,
            Categories = new List<string> { "Clinic", "Salon", "Tutor" },
            SelectedCategory = category,
            SearchTerm = search
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, [FromQuery] string? date, [FromQuery] Guid? serviceId)
    {
        var provider = await _providerService.GetByIdAsync(id);
        if (provider is null)
        {
            return NotFound("Provider not found.");
        }

        var services = await _providerService.GetServicesByProviderIdAsync(id, activeOnly: true);

        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var parsedDate))
        {
            targetDate = parsedDate;
        }

        var availableSlots = await _slotService.GetAvailableSlotsAsync(id, targetDate);

        var model = new ProviderDetailsViewModel
        {
            Provider = provider,
            Services = services,
            InitialDate = targetDate,
            AvailableSlots = availableSlots,
            PreselectedServiceId = serviceId
        };

        return View(model);
    }
}
