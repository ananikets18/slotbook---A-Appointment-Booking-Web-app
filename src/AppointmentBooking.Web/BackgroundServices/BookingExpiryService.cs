using AppointmentBooking.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppointmentBooking.Web.BackgroundServices;

/// <summary>
/// Background service (IHostedService) that periodically sweeps the database for bookings left in 'Pending'
/// status longer than the configured threshold (e.g., 30 minutes) and cancels them,
/// automatically releasing the reserved slots back into the available pool.
/// </summary>
public class BookingExpiryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BookingExpiryService> _logger;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _expiryThreshold;

    public BookingExpiryService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<BookingExpiryService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var intervalSeconds = configuration.GetValue("BookingExpiry:CheckIntervalSeconds", 60);
        var thresholdMinutes = configuration.GetValue("BookingExpiry:ThresholdMinutes", 30);

        _checkInterval = TimeSpan.FromSeconds(intervalSeconds);
        _expiryThreshold = TimeSpan.FromMinutes(thresholdMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingExpiryService started. Checking every {Interval} for pending bookings older than {Threshold}.",
            _checkInterval, _expiryThreshold);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredBookingsAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error occurred during BookingExpiryService sweep.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("BookingExpiryService has stopped.");
    }

    private async Task ProcessExpiredBookingsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var expiredCount = await bookingService.ExpireOldPendingBookingsAsync(_expiryThreshold, stoppingToken);
        if (expiredCount > 0)
        {
            _logger.LogInformation("BookingExpiryService automatically expired and released slots for {Count} unconfirmed booking(s).", expiredCount);
        }
    }
}
