# ------------------------------------------------------------------------------
#  HrmSystem Blazor WASM client — published as STATIC FILES behind nginx.
#  nginx also reverse-proxies /api and /hubs to the API container, making the
#  browser origin identical for REST + SignalR (no CORS in production).
#  Build context is the REPOSITORY ROOT:
#      docker build -f src/Client/HrmSystem.Client/Dockerfile .
# ------------------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["src/Client/HrmSystem.Client/HrmSystem.Client.csproj", "src/Client/HrmSystem.Client/"]
RUN dotnet restore "./src/Client/HrmSystem.Client/HrmSystem.Client.csproj"

COPY . .
WORKDIR "/src/src/Client/HrmSystem.Client"
RUN dotnet publish "./HrmSystem.Client.csproj" -c $BUILD_CONFIGURATION -o /app/publish

FROM nginx:alpine AS final
COPY containers/nginx/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 80
