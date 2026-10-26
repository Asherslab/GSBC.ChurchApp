using Contracts;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ProtoBuf.Grpc.Client;
using Wasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddSingleton(_ => GrpcChannel.ForAddress("http://localhost:5310",
    new GrpcChannelOptions { HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, new HttpClientHandler()) }));
builder.Services.AddSingleton(sp => sp.GetRequiredService<GrpcChannel>().CreateGrpcService<IEventsService>());
await builder.Build().RunAsync();
