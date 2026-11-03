using System.Text.Json;
using Grpc.Core;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Client.Diagnostics;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Native;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Signals;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Analytics.Client;

internal sealed class ErrorReporter(IServiceProvider services, AnalyticsConsent consent, CurrentPage page, TimeProvider time, ReportOrigin origin)
    : IClientDiagnostics, IClientStartupTask, IDisposable
{
    private static readonly TimeSpan GatherFor = TimeSpan.FromSeconds(3);
    private static readonly HashSet<StatusCode> NetworkStatuses = [StatusCode.Unavailable, StatusCode.DeadlineExceeded];

    private readonly Lock _lock = new();
    private readonly Dictionary<string, ErrorOccurrence> _errors = [];
    private readonly Dictionary<(int Day, string Method, int Status), FailedCallCount> _calls = [];
    private readonly NativeBridge? _bridge = services.GetService<NativeBridge>();
    private bool _scheduled;

    public async Task RunAsync()
    {
        await consent.LoadAsync();
        consent.Changed += OnConsentChanged;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandled;
        TaskScheduler.UnobservedTaskException += OnUnobserved;
        if (_bridge is { IsNative: true })
        {
            _bridge.CrashesArrived += OnCrashesArrived;
            await TakeNativeCrashesAsync();
        }
    }

    public void ExceptionThrown(Exception exception)
    {
        if (!consent.Enabled)
            return;

        List<string> frames = StackSummary.FramesOf(exception);
        Add(new ErrorOccurrence
        {
            Day = Today,
            ExceptionType = StackSummary.TypeOf(exception),
            Frames = frames,
            Route = page.Template ?? "",
            Count = 1,
        });
    }

    public void CallFailed(string method, StatusCode status)
    {
        if (!consent.Enabled || status == StatusCode.Cancelled || method == IDiagnosticsService.ReportMethod)
            return;
        if (NetworkStatuses.Contains(status) && services.GetService<ClientSignals>() is { Online: false })
            return;

        lock (_lock)
        {
            if (_calls.Count >= DiagnosticsReport.MaximumFailedCalls)
                return;
            (int, string, int) key = (Today, method, (int)status);
            if (!_calls.TryGetValue(key, out FailedCallCount? call))
                _calls[key] = call = new FailedCallCount { Day = Today, Method = method, Status = (int)status };
            call.Count++;
        }
        Schedule();
    }

    private int Today => ChurchTime.Today(time).DayNumber;

    private void Add(ErrorOccurrence error)
    {
        string key = $"{error.NativeCrash}|{error.ExceptionType}|{string.Join('|', error.Frames)}|{error.Route}|{error.AppBuild}";
        lock (_lock)
        {
            if (_errors.TryGetValue(key, out ErrorOccurrence? known))
                known.Count++;
            else if (_errors.Count < DiagnosticsReport.MaximumErrors)
                _errors[key] = error;
        }
        Schedule();
    }

    private void Schedule()
    {
        lock (_lock)
        {
            if (_scheduled)
                return;
            _scheduled = true;
        }
        _ = SendSoonAsync();
    }

    private async Task SendSoonAsync()
    {
        await Task.Delay(GatherFor, time);
        DiagnosticsReport report;
        lock (_lock)
        {
            _scheduled = false;
            report = new DiagnosticsReport { Context = Context(), Errors = [.. _errors.Values], FailedCalls = [.. _calls.Values] };
            _errors.Clear();
            _calls.Clear();
        }
        if (consent.Enabled && (report.Errors.Count > 0 || report.FailedCalls.Count > 0))
            await SendAsync(report);
    }

    private async Task SendAsync(DiagnosticsReport report)
    {
        try
        {
            if (services.GetService<IOutbox>() is { IsAvailable: true } outbox)
            {
                using MemoryStream body = new();
                AnalyticsContractsModel.Instance.Serialize(body, report);
                await outbox.EnqueueQuietlyAsync(IDiagnosticsService.ReportMethod, body.ToArray());
            }
            else
            {
                await services.GetRequiredService<ChannelBase>().CreateGrpcService<IDiagnosticsService>().ReportAsync(report);
            }
        }
        catch (Exception)
        {
        }
    }

    private ReportContext Context() => new()
    {
        Origin = origin,
        Platform = _bridge is { IsNative: true } ? _bridge.Platform == "android" ? DevicePlatform.Android : DevicePlatform.Ios : DevicePlatform.Web,
        AppVersion = _bridge?.AppVersion ?? "",
        AppBuild = _bridge?.AppBuild ?? "",
        OsMajor = _bridge?.OsMajor ?? 0,
        DevelopmentBuild = _bridge?.IsDevelopmentBuild ?? false,
    };

    private async Task TakeNativeCrashesAsync()
    {
        try
        {
            NativeCrashJson[] crashes = JsonSerializer.Deserialize(await _bridge!.TakeCrashesAsync(), CrashJson.Default.NativeCrashJsonArray) ?? [];
            if (!consent.Enabled)
                return;
            foreach (NativeCrashJson crash in crashes)
                Add(new ErrorOccurrence
                {
                    Day = ChurchTime.DayOf(DateTime.UnixEpoch.AddMilliseconds(crash.CrashedAt)).DayNumber,
                    ExceptionType = crash.Type,
                    Frames = [.. crash.Frames],
                    Count = 1,
                    NativeCrash = true,
                    AppVersion = crash.AppVersion,
                    AppBuild = crash.AppBuild,
                });
        }
        catch (Exception)
        {
        }
    }

    private void OnCrashesArrived() => _ = TakeNativeCrashesAsync();

    private void OnUnhandled(object? sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is Exception exception)
            ExceptionThrown(exception);
    }

    private void OnUnobserved(object? sender, UnobservedTaskExceptionEventArgs args) => ExceptionThrown(args.Exception);

    private void OnConsentChanged()
    {
        if (consent.Enabled)
            return;
        lock (_lock)
        {
            _errors.Clear();
            _calls.Clear();
        }
    }

    public void Dispose()
    {
        consent.Changed -= OnConsentChanged;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandled;
        TaskScheduler.UnobservedTaskException -= OnUnobserved;
        if (_bridge is not null)
            _bridge.CrashesArrived -= OnCrashesArrived;
    }
}
