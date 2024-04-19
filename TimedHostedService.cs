using BackGroundServiceTest;
using Microsoft.Extensions.Options;

public class TimedHostedService : IHostedService, IDisposable
{
    private readonly ILogger<TimedHostedService> _logger;
    private Timer? _timer = null;
    private readonly ScheduleConfig _scheduleConfig;

    public TimedHostedService(
        ILogger<TimedHostedService> logger,
        IOptions<ScheduleConfig> scheduleConfig)
    {
        _logger = logger;
        _scheduleConfig = scheduleConfig.Value;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service running.");
        ScheduleDailyTaskBasedOnUserSpecifiedTime();

        return Task.CompletedTask;
    }

    private void ScheduleDailyTaskBasedOnUserSpecifiedTime()
    {
        var currentTime = DateTime.UtcNow;
        TimeZoneInfo userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(_scheduleConfig.TimeZoneId);
        var currentTimeInUserTimeZone = TimeZoneInfo.ConvertTimeFromUtc(currentTime, userTimeZone);

        var targetTime = new DateTime(
            currentTimeInUserTimeZone.Year,
            currentTimeInUserTimeZone.Month,
            currentTimeInUserTimeZone.Day,
            _scheduleConfig.Hour,
            _scheduleConfig.Minute,
            0,
            DateTimeKind.Unspecified);

        if (currentTimeInUserTimeZone > targetTime)
        {
            targetTime = targetTime.AddDays(1);
        }

        targetTime = TimeZoneInfo.ConvertTimeToUtc(targetTime, userTimeZone);

        var timeToWait = targetTime - currentTime;

        _timer = new Timer(DoWork, null, timeToWait, TimeSpan.FromDays(1));
    }

    private void DoWork(object? state)
    {
        _logger.LogInformation("Timed Hosted Service is working.");

        // Reschedule the next run
        ScheduleDailyTaskBasedOnUserSpecifiedTime();
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service is stopping.");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
