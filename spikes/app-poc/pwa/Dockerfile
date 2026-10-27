# Short-lived PWA build of the design spike, so staff can try it before TestFlight. Build context: spikes/app-poc.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# AOT needs the wasm-tools workload from this exact SDK, and its Emscripten toolchain needs python3.
RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 \
    && rm -rf /var/lib/apt/lists/* \
    && dotnet workload install wasm-tools

COPY Directory.Build.props ./
COPY App/App.csproj App/
RUN dotnet restore App/App.csproj

COPY App/ App/
RUN dotnet publish App/App.csproj -c Release -o /app/publish --no-restore \
    && test -f /app/publish/wwwroot/index.html

FROM nginx:1.27-alpine AS final
COPY pwa/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 8080
