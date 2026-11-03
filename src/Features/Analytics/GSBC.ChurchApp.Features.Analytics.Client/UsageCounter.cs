using System.Text.Json;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Client.Analytics;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Native;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Analytics;

namespace GSBC.ChurchApp.Features.Analytics.Client;

internal sealed class UsageCounter(NativeBridge bridge, IOutbox outbox, ILocalPreferences preferences, AnalyticsConsent consent, TimeProvider time)
    : IUsage, IClientStartupTask, IDisposable
{
    private const string PreferenceKey = "analytics.usage";
    private const int MaximumPending = 400;
    private static readonly TimeSpan ReportAtMostEvery = TimeSpan.FromHours(1);

    private UsageState _state = new();
    private bool _started;

    public async Task RunAsync()
    {
        if (!bridge.IsNative || !outbox.IsAvailable)
            return;

        _state = await LoadAsync();
        _started = true;
        consent.Changed += OnConsentChanged;
        bridge.AppStateChanged += OnAppStateChanged;
        await OpenedAsync();
    }

    public void Count(Measure measure, string? subject = null)
    {
        if (!_started || !consent.Enabled)
            return;
        Add(Today, measure, subject ?? "");
        _ = SaveAsync();
    }

    private int Today => ChurchTime.Today(time).DayNumber;

    private async Task OpenedAsync()
    {
        if (!consent.Enabled)
            return;

        DateOnly today = ChurchTime.Today(time);
        DateOnly weekStart = ChurchTime.WeekStart(today);
        bool newDay = _state.Opened.LastDay != today.DayNumber;
        if (_state.Opened.IsNewWeek(weekStart))
            _state.WeekGroup = (int)(bridge.SignedIn ? UsageGroup.SignedIn : UsageGroup.NotSignedIn);
        foreach (Measure measure in _state.Opened.Open(today, weekStart))
            Add(today.DayNumber, measure, "");

        if (newDay)
            await ReportAsync();
        else
            await SaveAsync();
    }

    private void Add(int day, Measure measure, string subject)
    {
        int group = measure.SplitByGroup ? _state.WeekGroup : (int)UsageGroup.Everyone;
        PendingCount? known = _state.Pending.FirstOrDefault(pending =>
            pending.Day == day && pending.Measure == measure.Key && pending.Subject == subject && pending.Group == group);
        if (known is not null)
            known.Count = measure.OncePerPhone ? 1 : known.Count + 1;
        else if (_state.Pending.Count < MaximumPending)
            _state.Pending.Add(new PendingCount { Day = day, Measure = measure.Key, Subject = subject, Group = group, Count = 1 });
    }

    private async Task ReportAsync()
    {
        if (_state.Pending.Count > 0)
        {
            UsageReport report = new()
            {
                Context = new ReportContext
                {
                    Origin = ReportOrigin.App,
                    Platform = bridge.Platform == "android" ? DevicePlatform.Android : DevicePlatform.Ios,
                    AppVersion = bridge.AppVersion ?? "",
                    AppBuild = bridge.AppBuild ?? "",
                    OsMajor = bridge.OsMajor,
                    DevelopmentBuild = bridge.IsDevelopmentBuild,
                },
                Counts = _state.Pending.Take(UsageReport.MaximumCounts).Select(pending => new UsageCount
                {
                    Day = pending.Day,
                    Measure = pending.Measure,
                    Subject = pending.Subject,
                    Group = (UsageGroup)pending.Group,
                    Count = pending.Count,
                }).ToList(),
            };
            using MemoryStream body = new();
            AnalyticsContractsModel.Instance.Serialize(body, report);
            await outbox.EnqueueQuietlyAsync(IAnalyticsService.ReportMethod, body.ToArray());
            _state.Pending.Clear();
        }
        _state.LastReportAt = time.GetUtcNow().ToUnixTimeMilliseconds();
        await SaveAsync();
    }

    private void OnAppStateChanged(bool active) => _ = active ? OpenedAsync() : ReportIfDueAsync();

    private async Task ReportIfDueAsync()
    {
        if (consent.Enabled && time.GetUtcNow().ToUnixTimeMilliseconds() - _state.LastReportAt >= (long)ReportAtMostEvery.TotalMilliseconds)
            await ReportAsync();
    }

    private void OnConsentChanged()
    {
        if (consent.Enabled)
            return;
        _state.Pending.Clear();
        _ = SaveAsync();
    }

    private async Task<UsageState> LoadAsync()
    {
        try
        {
            return await preferences.GetAsync(PreferenceKey) is { } json ? JsonSerializer.Deserialize(json, UsageJson.Default.UsageState) ?? new() : new();
        }
        catch (JsonException)
        {
            return new();
        }
    }

    private async Task SaveAsync() => await preferences.SetAsync(PreferenceKey, JsonSerializer.Serialize(_state, UsageJson.Default.UsageState));

    public void Dispose()
    {
        consent.Changed -= OnConsentChanged;
        bridge.AppStateChanged -= OnAppStateChanged;
    }
}
