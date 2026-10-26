# Spike: code-first gRPC under AOT

**Question:** can we keep code-first gRPC (protobuf-net.Grpc, plain C# interfaces, no `.proto`) when
both the backend and the Blazor app are compiled AOT? **Answer: yes.** It needs protobuf-net's
compile-time generators, which are still in preview. The findings and the rules they lead to are in
[SPEC.md §7](../../SPEC.md#7-aot-from-the-first-line).

The spike was run on 2026-10-26 with .NET SDK 10.0.401 (runtime 10.0.12), protobuf-net 3.4.40,
protobuf-net.Grpc 1.3.14 and Grpc.Net.Client(.Web) 2.84.0.

| Project | What it is |
|---|---|
| `Contracts` | Messages and an `[Service]` interface (unary and server-streaming), plus the generated `[ProtoModel]` and client `[ProtoGrpc]` |
| `Server` | ASP.NET Core, `PublishAot`, generated `AddServerServices()`, gRPC-Web |
| `Client` | A console app, `PublishAot`, calling over gRPC-Web |
| `Wasm` | Blazor WebAssembly, `RunAOTCompilation` + `WasmStripILAfterAOT` |

## Results

| Run | Result |
|---|---|
| Native AOT server + native AOT client, generated model and proxies | Unary and streaming calls work |
| The same client without the generators (reflection path) | Crashes at start-up: `PlatformNotSupportedException: Dynamic code generation is not supported` |
| Blazor WASM AOT (`IsDynamicCodeCompiled == false`) in Chrome, against the AOT server | Unary and streaming calls work |
| Blazor WASM with `IsAotCompatible` set on the *app* project | Broken: the trimmer removed the routed pages (`CtorNotLocated`) |

The generators matter: without them a native AOT build compiles cleanly and then fails when it runs.

## Re-running

```sh
dotnet publish Server -c Release -r osx-arm64 -o out/server
dotnet publish Client -c Release -r osx-arm64 -o out/client
dotnet publish Wasm   -c Release -o out/wasm
ASPNETCORE_URLS=http://localhost:5310 ./out/server/Server &
./out/client/Client http://localhost:5310
python3 -m http.server 5311 -d out/wasm/wwwroot   # then open http://localhost:5311
```

On macOS with Homebrew's .NET, native linking needs the Homebrew OpenSSL and Brotli libraries. The
`LinkerArg` in `Directory.Build.props` supplies them. On Linux and in Docker it isn't needed.
