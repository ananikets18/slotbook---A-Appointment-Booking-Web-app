using AppointmentBooking.Application.Services;
using AppointmentBooking.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Web.Controllers;

public class HomeController : Controller
{
    private readonly IProviderService _providerService;
    private readonly IBookingService _bookingService;

    public HomeController(IProviderService providerService, IBookingService bookingService)
    {
        _providerService = providerService;
        _bookingService = bookingService;
    }

    public async Task<IActionResult> Index([FromQuery] string? category)
    {
        var allProviders = await _providerService.GetProvidersAsync(category);
        var categories = new List<string> { "Clinic", "Salon", "Tutor" };

        var model = new HomeViewModel
        {
            FeaturedProviders = allProviders.Take(6).ToList(),
            Categories = categories,
            TotalProviders = allProviders.Count,
            SelectedCategory = category
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Error(int? statusCode, string? message)
    {
        ViewBag.StatusCode = statusCode ?? 500;
        ViewBag.ErrorMessage = message ?? "An unexpected error occurred.";
        return View();
    }
}
