# ------------------------------------------------------------------------------
#  HrmSystem API — multi-stage build (pattern ported from the MilestoneStack Web
#  Dockerfile). Build context is the REPOSITORY ROOT:
#      docker build -f src/Web/HrmSystem.Web/Dockerfile .
# ------------------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restore first with only the project/props files so layer caching survives code edits.
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["src/Web/HrmSystem.Web/HrmSystem.Web.csproj", "src/Web/HrmSystem.Web/"]
COPY ["src/Core/HrmSystem.Application/HrmSystem.Application.csproj", "src/Core/HrmSystem.Application/"]
COPY ["src/Core/HrmSystem.Domain/HrmSystem.Domain.csproj", "src/Core/HrmSystem.Domain/"]
COPY ["src/Infrastructure/HrmSystem.Infrastructure/HrmSystem.Infrastructure.csproj", "src/Infrastructure/HrmSystem.Infrastructure/"]
RUN dotnet restore "./src/Web/HrmSystem.Web/HrmSystem.Web.csproj"

COPY . .
WORKDIR "/src/src/Web/HrmSystem.Web"
RUN dotnet build "./HrmSystem.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HrmSystem.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HrmSystem.Web.dll"]
