using System.Security.Claims;
using AppointmentBooking.Application.DTOs;
using AppointmentBooking.Application.Services;
using AppointmentBooking.Core.Enums;
using AppointmentBooking.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Web.Controllers.Api;

[ApiController]
[Route("api/bookings")]
[Produces("application/json")]
public class BookingsApiController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsApiController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Books a specific 30-minute slot for a chosen service.
    /// Employs optimistic concurrency and database transactions to strictly prevent double-booking.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new { error = "You must be signed in to book an appointment." });
        }

        try
        {
            var booking = await _bookingService.CreateBookingAsync(userId, request);
            return CreatedAtAction(nameof(GetBookingById), new { id = booking.Id }, booking);
        }
        catch (ConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets appointment details by Booking ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingById(Guid id)
    {
        var booking = await _bookingService.GetByIdAsync(id);
        if (booking is null)
        {
            return NotFound(new { error = "Booking not found." });
        }

        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        if (userRole == UserRole.Customer && booking.UserId != userId)
        {
            return Forbid();
        }

        return Ok(booking);
    }

    /// <summary>
    /// Updates appointment status following domain transition rules (Pending -> Confirmed -> Completed, or Cancelled).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateBookingStatusRequest request)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        try
        {
            var updated = await _bookingService.UpdateStatusAsync(id, userId, userRole, request);
            return Ok(updated);
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
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
