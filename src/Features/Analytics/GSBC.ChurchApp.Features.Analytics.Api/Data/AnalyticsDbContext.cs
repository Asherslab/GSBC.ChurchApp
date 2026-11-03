using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Analytics.Api.Data;

public sealed class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "analytics";

    public DbSet<ErrorKindRecord> ErrorKinds => Set<ErrorKindRecord>();
    public DbSet<ErrorReportRecord> ErrorReports => Set<ErrorReportRecord>();
    public DbSet<FailedCallRecord> FailedCalls => Set<FailedCallRecord>();
    public DbSet<SeenReportRecord> SeenReports => Set<SeenReportRecord>();
    public DbSet<DailyCountRecord> DailyCounts => Set<DailyCountRecord>();
    public DbSet<MonthlyCountRecord> MonthlyCounts => Set<MonthlyCountRecord>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ErrorKindRecord>(entity =>
        {
            entity.ToTable("error_kinds");
            entity.HasKey(kind => kind.Id);
            entity.HasIndex(kind => kind.Fingerprint).IsUnique();
            entity.HasIndex(kind => kind.LastSeen);
        });
        modelBuilder.Entity<ErrorReportRecord>(entity =>
        {
            entity.ToTable("error_reports");
            entity.HasKey(report => report.Id);
            entity.HasIndex(report => new { report.KindId, report.Day });
            entity.HasIndex(report => report.ReceivedAt);
            entity.HasOne<ErrorKindRecord>().WithMany().HasForeignKey(report => report.KindId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<FailedCallRecord>(entity =>
        {
            entity.ToTable("failed_calls");
            entity.HasKey(call => new { call.Day, call.Origin, call.Method, call.Status, call.AppVersion });
        });
        modelBuilder.Entity<DailyCountRecord>(entity =>
        {
            entity.ToTable("daily_counts");
            entity.HasKey(count => new { count.Day, count.Measure, count.Subject, count.Group, count.Platform, count.AppVersion });
            entity.HasIndex(count => new { count.Measure, count.Day });
        });
        modelBuilder.Entity<MonthlyCountRecord>(entity =>
        {
            entity.ToTable("monthly_counts");
            entity.HasKey(count => new { count.Month, count.Measure, count.Subject, count.Group, count.Platform, count.AppVersion });
        });
        modelBuilder.Entity<SeenReportRecord>(entity =>
        {
            entity.ToTable("reports_seen");
            entity.HasKey(seen => seen.RequestId);
            entity.HasIndex(seen => seen.SeenAt);
        });
    }
}
