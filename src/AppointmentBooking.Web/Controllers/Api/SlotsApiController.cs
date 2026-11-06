using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Web.Controllers.Api;

[ApiController]
[Route("api/slots")]
[Produces("application/json")]
public class SlotsApiController : ControllerBase
{
    private readonly ISlotService _slotService;

    public SlotsApiController(ISlotService slotService)
    {
        _slotService = slotService;
    }

    /// <summary>
    /// Gets available (unbooked) 30-minute slots for a given provider on a specific date.
    /// Results are cached using IMemoryCache with sliding expiration.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SlotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] Guid providerId,
        [FromQuery] string date)
    {
        if (providerId == Guid.Empty)
        {
            return BadRequest(new { error = "A valid providerId is required." });
        }

        if (!DateOnly.TryParse(date, out var parsedDate))
        {
            return BadRequest(new { error = "Invalid date format. Expected yyyy-MM-dd." });
        }

        var slots = await _slotService.GetAvailableSlotsAsync(providerId, parsedDate);
        return Ok(slots);
    }

    /// <summary>
    /// Gets all slots (both booked and unbooked) for a given provider on a specific date.
    /// </summary>
    [HttpGet("all")]
    [ProducesResponseType(typeof(List<SlotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSlots(
        [FromQuery] Guid providerId,
        [FromQuery] string date)
    {
        if (providerId == Guid.Empty)
        {
            return BadRequest(new { error = "A valid providerId is required." });
        }

        if (!DateOnly.TryParse(date, out var parsedDate))
        {
            return BadRequest(new { error = "Invalid date format. Expected yyyy-MM-dd." });
        }

        var slots = await _slotService.GetAllSlotsForDateAsync(providerId, parsedDate);
        return Ok(slots);
    }
}
