# Spike: code-first gRPC under AOT

**Question:** can we keep code-first gRPC (protobuf-net.Grpc, plain C# interfaces, no `.proto`) when
both the backend and the Blazor app are compiled AOT? **Answer: yes.** It needs protobuf-net's
compile-time generators, which are still in preview. The rules that follow from this spike are in
[SPEC.md §8](../../SPEC.md#8-aot-from-the-first-line).

Run on 2026-10-26 with .NET SDK 10.0.401 (runtime 10.0.12), protobuf-net 3.4.40,
protobuf-net.Grpc 1.3.14 and Grpc.Net.Client(.Web) 2.84.0.

| Project | What it is |
|---|---|
| `Contracts` | Messages and an `[Service]` interface (unary + server-streaming), the `[ProtoModel]`, Level300 |
| `Server` | ASP.NET Core, native AOT. Generated `AddServerServices()`. Ports 5310 (gRPC-Web, HTTP/1.1+2) and 5312 (native gRPC, HTTP/2) |
| `Client` | Console app, native AOT. Calls over both transports through its own generated proxies |
| `Wasm` | Blazor WebAssembly, `RunAOTCompilation` + `WasmStripILAfterAOT`, calls over gRPC-Web |
| `aot-warnings/` | Known library warnings per host, and the check that enforces them |
| `wasm-check.mjs` | Loads a page in headless Chrome over the DevTools protocol and prints `#out` |

All projects are `IsAotCompatible` and `TreatWarningsAsErrors`.

## Results

| Run | Result |
|---|---|
| Native AOT server → native AOT client, native gRPC over HTTP/2 | Unary and streaming work |
| The same, over gRPC-Web on HTTP/1.1 | Unary and streaming work |
| Blazor WASM AOT (`IsDynamicCodeCompiled == false`) in Chrome → AOT server | Unary and streaming work |
| A client without the generators (reflection path) | Builds, then crashes at start-up: `PlatformNotSupportedException` |
| Blazor app with `IsAotCompatible` and its assembly **not** rooted | Broken: routed pages trimmed (`CtorNotLocated`) |
| The same with `<TrimmerRootAssembly Include="$(AssemblyName)" />` | Works, with analysis still on |
| Client factory declared only in `Contracts`, protobuf-net only transitive | Builds silently, crashes under AOT |
| The same with a direct `protobuf-net` reference | Build fails with `PBN4015`, as it should |
| `DateTime` sent UTC, default format | Arrives as `Kind=Unspecified` |
| `DateTime` sent UTC, `CompatibilityLevel.Level300` | Arrives as `Kind=Utc` |
| Member suppression of a library warning via `--link-attributes` (WASM trimmer) | Works, but only through the unofficial `_ExtraTrimmerArgs` |
| Embedded `ILLink.LinkAttributes.xml` under native AOT | Ignored (5 warnings before, 5 after) |

Warning baselines on the final code: 39 library warnings (server), 39 (client), 222 (WASM). None are
in our code. Two that were in our code are suppressed in source with a justification: the Razor
compiler's `Router.NotFoundPage` pair in `App.razor`, and protobuf-net's `IL2095` annotation mismatch,
which it reports on our `ContractsModel`.

## Re-running

```sh
dotnet publish Server -c Release -r osx-arm64 -o out/server > out/server.publish.log
dotnet publish Client -c Release -r osx-arm64 -o out/client > out/client.publish.log
dotnet publish Wasm   -c Release -o out/wasm > out/wasm.publish.log
aot-warnings/check-all.sh                     # --update to regenerate after an upgrade
./out/server/Server &                         # 5310 gRPC-Web, 5312 native gRPC
./out/client/Client                           # calls over both
python3 -m http.server 5311 -d out/wasm/wwwroot &
PROFILE=$(mktemp -d) node wasm-check.mjs http://localhost:5311/
```

On macOS with Homebrew's .NET, native linking needs Homebrew's OpenSSL and Brotli. The `LinkerArg` in
`Directory.Build.props` supplies them. On Linux and in Docker it isn't needed.
