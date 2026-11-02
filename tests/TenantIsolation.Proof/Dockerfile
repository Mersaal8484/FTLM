FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY tests/TenantIsolation.Proof/TenantIsolation.Proof.csproj tests/TenantIsolation.Proof/
ARG NUGET_SOURCE=https://api.nuget.org/v3/index.json
RUN dotnet restore tests/TenantIsolation.Proof/TenantIsolation.Proof.csproj --source "$NUGET_SOURCE"
COPY tests/TenantIsolation.Proof/Program.cs tests/TenantIsolation.Proof/
COPY src/FuelTrack.Domain/MultiTenancy/TenantIdentity.cs src/FuelTrack.Domain/MultiTenancy/
COPY src/FuelTrack.Domain/MultiTenancy/PlatformTenant.cs src/FuelTrack.Domain/MultiTenancy/
COPY src/FuelTrack.Domain/MultiTenancy/SubscriptionPlan.cs src/FuelTrack.Domain/MultiTenancy/
COPY src/FuelTrack.Domain/MultiTenancy/PlatformSubscription.cs src/FuelTrack.Domain/MultiTenancy/
COPY src/FuelTrack.Platform/ src/FuelTrack.Platform/
RUN dotnet publish tests/TenantIsolation.Proof/TenantIsolation.Proof.csproj --no-restore -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "TenantIsolation.Proof.dll"]
