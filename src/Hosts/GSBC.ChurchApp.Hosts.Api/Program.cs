using GSBC.ChurchApp.Features.Accounts.Api;
using GSBC.ChurchApp.Platform.Api.Hosting;
using Grpc.AspNetCore.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddPlatformApi();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddAccountsApi();

WebApplication app = builder.Build();

app.UseAuthentication();
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
app.MapPlatformApi();
app.MapAccountsApi();

app.Run();
