using Auth.Application.Emails;

namespace Auth.Api.Workers;

public sealed class EmailOutboxWorker(
    IServiceScopeFactory scopeFactory,
    EmailWorkerOptions options,
    ILogger<EmailOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.PollSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SendDueEmailsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task SendDueEmailsAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var sendPendingEmails = scope.ServiceProvider.GetRequiredService<SendPendingEmails>();
            await sendPendingEmails.ExecuteAsync(stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Email outbox tick failed");
        }
    }
}
