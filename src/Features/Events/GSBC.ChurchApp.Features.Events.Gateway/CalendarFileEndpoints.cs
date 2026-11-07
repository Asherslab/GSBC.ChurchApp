using Grpc.Core;
using Grpc.Net.Client;
using GSBC.ChurchApp.Features.Events.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Events.Gateway;

public static class CalendarFileEndpoints
{
    public const string ApiAddressSetting = "ReverseProxy:Clusters:api-grpc:Destinations:api:Address";
    public const string SharedCaching = "public, max-age=900";
    public const string SecretCaching = "private, no-store";
    public const string NotCached = "no-store";

    public static IServiceCollection AddEventsGateway(this IServiceCollection services, IConfiguration configuration) =>
        services.AddSingleton(_ => GrpcChannel.ForAddress(configuration[ApiAddressSetting] ?? "http://api:8081").CreateGrpcService<ICalendarFilesService>());

    public static IEndpointRouteBuilder MapCalendarFiles(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/calendars/public.ics", (HttpContext http, ICalendarFilesService files) =>
            ServeAsync(http, files, new CalendarFileRequest { Calendar = CalendarLinks.Public }, SharedCaching));
        endpoints.MapGet("/calendars/{calendar}.ics", (string calendar, HttpContext http, ICalendarFilesService files) =>
            ServeAsync(http, files, new CalendarFileRequest { Calendar = calendar }, SharedCaching));
        endpoints.MapGet("/calendars/mine/{secret}.ics", (string secret, HttpContext http, ICalendarFilesService files) =>
            ServeAsync(http, files, new CalendarFileRequest { Secret = secret }, SecretCaching));
        endpoints.MapGet("/calendars/mine/{secret}/{calendar}.ics", (string secret, string calendar, HttpContext http, ICalendarFilesService files) =>
            ServeAsync(http, files, new CalendarFileRequest { Secret = secret, Calendar = calendar }, SecretCaching));
        return endpoints;
    }

    private static async Task<IResult> ServeAsync(HttpContext http, ICalendarFilesService files, CalendarFileRequest request, string caching)
    {
        http.Response.Headers.CacheControl = NotCached;
        CalendarFile file;
        try
        {
            file = await files.ReadAsync(request, new ProtoBuf.Grpc.CallContext(new CallOptions(cancellationToken: http.RequestAborted)));
        }
        catch (RpcException exception) when (exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        if (!file.Found)
            return Results.NotFound();
        http.Response.Headers.CacheControl = caching;
        return Results.Text(file.Text, CalendarLinks.ContentType);
    }
}
