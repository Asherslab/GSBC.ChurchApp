using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Email;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed class NewErrorKinds(
    AnalyticsDbContext db, IPermissionHolders holders, IAccountEmails accounts, IEmailSender email,
    AnalyticsEnvironment environment, TimeProvider time, ILogger<NewErrorKinds> logger)
{
    public const int MaximumEmailsPerDay = 10;

    public async Task AnnounceAsync(IReadOnlyCollection<Guid> kinds, CancellationToken cancellationToken)
    {
        if (kinds.Count == 0)
            return;

        try
        {
            DateTime now = time.GetUtcNow().UtcDateTime;
            DateTime startOfDay = ChurchTime.ToUtc(ChurchTime.Today(time).ToDateTime(TimeOnly.MinValue));
            int sentToday = await db.ErrorKinds.CountAsync(kind => kind.NotifiedAt >= startOfDay, cancellationToken);
            if (sentToday >= MaximumEmailsPerDay)
                return;

            IReadOnlyList<string> recipients = await accounts.EmailsOfAsync(
                await holders.HoldersOfAsync(AnalyticsPermissions.HealthViewNode, cancellationToken), cancellationToken);

            List<ErrorKindRecord> announced = await db.ErrorKinds
                .Where(kind => kinds.Contains(kind.Id) && kind.NotifiedAt == null)
                .OrderBy(kind => kind.FirstSeen)
                .Take(MaximumEmailsPerDay - sentToday)
                .ToListAsync(cancellationToken);

            foreach (ErrorKindRecord kind in announced)
            {
                kind.NotifiedAt = now;
                foreach (string recipient in recipients)
                    await email.SendAsync(Describe(kind, recipient), cancellationToken);
            }
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Couldn't email about a new kind of error");
        }
    }

    private EmailMessage Describe(ErrorKindRecord kind, string recipient) => new(recipient,
        $"New error on {environment.Name}: {ShortName(kind.ExceptionType)}",
        $"""
        A kind of error that hasn't been seen before has just been reported.

        What: {kind.ExceptionType}
        Where: {kind.Where}
        From: {ErrorWords.Origin(kind.Origin, kind.NativeCrash)}

        See it on App health: {environment.SiteUrl}/health/{kind.Id}

        You get this because you can see app errors. Each new kind of error is emailed once.
        """);

    private static string ShortName(string type) => type[(type.LastIndexOf('.') + 1)..];
}
