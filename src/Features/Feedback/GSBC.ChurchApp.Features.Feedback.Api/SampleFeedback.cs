using GSBC.ChurchApp.Features.Feedback.Api.Data;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Feedback.Api;

internal sealed class SampleFeedback(IServiceScopeFactory scopes, IHostEnvironment environment, TimeProvider time, ILogger<SampleFeedback> logger)
    : BackgroundService
{
    private static readonly (string Text, string Name, int DaysAgo, TicketStatus Status, string Model, string Page)[] Samples =
    [
        ("The Save button on the event page is cut off at the bottom, under the tab bar.", "Sam Sample", 0, TicketStatus.New, "iPhone15,4", "/events"),
        ("Would be great to see the sermon notes when I'm offline at camp.", "", 1, TicketStatus.New, "iPhone13,2", "/sermons"),
        ("Audio stops when the screen locks.", "Jo Sample", 1, TicketStatus.InProgress, "Pixel 8", "/listen"),
        ("Tile text overlaps on a small phone.", "Riley Sample", 3, TicketStatus.FixedToCheck, "iPhone14,6", "/"),
        ("The dark mode icon for Prayer is missing.", "Pat Sample", 9, TicketStatus.Resolved, "iPhone15,4", "/prayer"),
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!environment.IsDevelopment())
            return;
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            FeedbackDbContext db = scope.ServiceProvider.GetRequiredService<FeedbackDbContext>();
            if (await db.Tickets.AnyAsync(stoppingToken))
                return;
            DateTime now = time.GetUtcNow().UtcDateTime;
            foreach ((string text, string name, int daysAgo, TicketStatus status, string model, string page) in Samples)
            {
                DateTime sent = now.AddDays(-daysAgo).AddHours(-2);
                TicketRecord ticket = new()
                {
                    Id = Guid.NewGuid(), Text = text, SenderName = name, SentAt = sent, ChangedAt = sent, Status = status,
                    ClosedAt = FeedbackRules.IsClosed(status) ? sent.AddHours(5) : null, AttachmentsDeleted = FeedbackRules.IsClosed(status),
                    AppVersion = "1.4", AppBuild = "212", Platform = model.StartsWith("Pixel", StringComparison.Ordinal) ? "android" : "ios",
                    OsVersion = model.StartsWith("Pixel", StringComparison.Ordinal) ? "15" : "18.1", Model = model, Environment = environment.EnvironmentName, Page = page,
                };
                ticket.Steps.Add(new StepRecord { At = sent, What = "Sent" });
                db.Tickets.Add(ticket);
                FeedbackMarks.Changed(db, ticket.Id, now);
            }
            await db.SaveChangesAsync(stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Adding the sample feedback failed");
        }
    }
}
