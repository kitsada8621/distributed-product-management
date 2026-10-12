using Auth.Application.Emails;
using Auth.Domain.Emails;
using Auth.Domain.Users;
using Auth.Infrastructure.Persistence;
using Auth.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Auth.IntegrationTests.Workers;

public class EmailOutboxWorkerTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Worker_SendsQueuedEmailAndMarksItSent()
    {
        await using var fastFactory = CreateFastFactory();
        using var client = fastFactory.CreateClient();
        var emailId = await QueueEmailAsync(fastFactory, "deliver@example.com");

        await WaitUntilAsync(async () => (await LoadAsync(fastFactory, emailId)).Status == EmailStatus.Sent);

        Assert.Contains(factory.EmailSender.Sent, e => e.To == "deliver@example.com");
        Assert.NotNull((await LoadAsync(fastFactory, emailId)).SentAt);
    }

    [Fact]
    public async Task Worker_SenderFails_KeepsRowPendingWithBackoffAndKeepsRunning()
    {
        var sender = new ToggleEmailSender { Fail = true };
        await using var fastFactory = CreateFastFactory(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(sender);
        });
        using var client = fastFactory.CreateClient();
        var failingId = await QueueEmailAsync(fastFactory, "fails@example.com");

        await WaitUntilAsync(async () => (await LoadAsync(fastFactory, failingId)).Attempts == 1);
        var failing = await LoadAsync(fastFactory, failingId);
        Assert.Equal(EmailStatus.Pending, failing.Status);
        Assert.True(failing.NextAttemptAt > failing.CreatedAt);

        sender.Fail = false;
        var healthyId = await QueueEmailAsync(fastFactory, "works@example.com");
        await WaitUntilAsync(async () => (await LoadAsync(fastFactory, healthyId)).Status == EmailStatus.Sent);
        Assert.Contains("works@example.com", sender.SentTo);
    }

    private WebApplicationFactory<Program> CreateFastFactory(Action<IServiceCollection>? configureServices = null)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["EmailWorker:PollSeconds"] = "1" }));
            if (configureServices is not null)
            {
                builder.ConfigureTestServices(configureServices);
            }
        });
    }

    private static async Task<Guid> QueueEmailAsync(WebApplicationFactory<Program> host, string to)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var user = User.Create(to, "Worker Test", "hash", UserRole.User, DateTime.UtcNow);
        var email = EmailOutbox.Create(user.Id, to, "Subject", "Body", DateTime.UtcNow);
        context.Users.Add(user);
        context.EmailOutboxes.Add(email);
        await context.SaveChangesAsync();
        return email.Id;
    }

    private static async Task<EmailOutbox> LoadAsync(WebApplicationFactory<Program> host, Guid emailId)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await context.EmailOutboxes.AsNoTracking().SingleAsync(e => e.Id == emailId);
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(200);
        }

        Assert.Fail($"Condition was not met within {Timeout.TotalSeconds} seconds");
    }

    private sealed class ToggleEmailSender : IEmailSender
    {
        private readonly List<string> _sentTo = [];

        public volatile bool Fail;

        public IReadOnlyCollection<string> SentTo
        {
            get
            {
                lock (_sentTo)
                {
                    return _sentTo.ToList();
                }
            }
        }

        public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
        {
            if (Fail)
            {
                throw new InvalidOperationException("smtp down");
            }

            lock (_sentTo)
            {
                _sentTo.Add(toEmail);
            }
            return Task.CompletedTask;
        }
    }
}
