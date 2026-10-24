# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for caching layer
COPY HomeMatch.sln ./
COPY src/HomeMatch.Api/HomeMatch.Api.csproj src/HomeMatch.Api/
COPY src/HomeMatch.Shared/HomeMatch.Shared.csproj src/HomeMatch.Shared/
COPY src/Modules/Identity/HomeMatch.Modules.Identity.csproj src/Modules/Identity/
COPY src/Modules/Properties/HomeMatch.Modules.Properties.csproj src/Modules/Properties/
COPY src/Modules/Listings/HomeMatch.Modules.Listings.csproj src/Modules/Listings/
COPY src/Modules/Viewings/HomeMatch.Modules.Viewings.csproj src/Modules/Viewings/
COPY src/Modules/Messaging/HomeMatch.Modules.Messaging.csproj src/Modules/Messaging/
COPY src/Modules/Leasing/HomeMatch.Modules.Leasing.csproj src/Modules/Leasing/
COPY src/Modules/Billing/HomeMatch.Modules.Billing.csproj src/Modules/Billing/
COPY src/Modules/Notifications/HomeMatch.Modules.Notifications.csproj src/Modules/Notifications/
COPY tests/UnitTests/HomeMatch.UnitTests.csproj tests/UnitTests/
COPY tests/IntegrationTests/HomeMatch.IntegrationTests.csproj tests/IntegrationTests/

# Restore dependencies
RUN dotnet restore HomeMatch.sln

# Copy the remaining source files and build
COPY . .
WORKDIR /src/src/HomeMatch.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HomeMatch.Api.dll"]
