using System.Security.Cryptography;
using GSBC.ChurchApp.Hosts.AppHost;
using Microsoft.Extensions.Configuration;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

using ECDsa tokenKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);

IResourceBuilder<PostgresDatabaseResource> database = builder
    .AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("churchapp");

IResourceBuilder<ProjectResource> migrator = builder
    .AddProject<Projects.GSBC_ChurchApp_Hosts_Migrator>("migrator")
    .WithReference(database)
    .WaitFor(database);

IResourceBuilder<ProjectResource> api = builder
    .AddProject<Projects.GSBC_ChurchApp_Hosts_Api>("api", launchProfileName: null)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithHttpEndpoint(name: "web", env: "Ports__Web")
    .WithHttpEndpoint(name: "grpc", env: "Ports__Grpc")
    .WithReference(database)
    .WithEnvironment("Accounts__Tokens__PublicKey", tokenKey.ExportSubjectPublicKeyInfoPem())
    .WaitForCompletion(migrator);

IConfigurationSection blobs = builder.Configuration.GetSection("Storage:Blobs");
if (blobs.Exists())
{
    foreach (IConfigurationSection setting in blobs.GetChildren())
        api.WithEnvironment($"Storage__Blobs__{setting.Key}", setting.Value);
}
else
{
    builder.AddDevMediaStore(api);
}

foreach (IConfigurationSection setting in builder.Configuration.GetSection("Email:Graph").GetChildren())
    api.WithEnvironment($"Email__Graph__{setting.Key}", setting.Value);

IResourceBuilder<ProjectResource> gateway = builder
    .AddProject<Projects.GSBC_ChurchApp_Hosts_Gateway>("gateway", launchProfileName: null)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithHttpEndpoint(port: 5300, name: "web", env: "Ports__Web", isProxied: false)
    .WithHttpEndpoint(port: 5301, name: "grpc", env: "Ports__Grpc", isProxied: false)
    .WithReference(database)
    .WithEnvironment("Accounts__Tokens__SigningKey", tokenKey.ExportPkcs8PrivateKeyPem())
    .WithEnvironment("ReverseProxy__Clusters__api-web__Destinations__api__Address", api.GetEndpoint("web"))
    .WithEnvironment("ReverseProxy__Clusters__api-grpc__Destinations__api__Address", api.GetEndpoint("grpc"))
    .WithExternalHttpEndpoints()
    .WaitFor(api);

if (DevTunnel.Find() is { } tunnel)
{
    builder
        .AddExecutable("tunnel", tunnel.Command, tunnel.WorkingDirectory, "tunnel", "run", tunnel.Name)
        .WaitFor(gateway);
}

builder.Build().Run();
