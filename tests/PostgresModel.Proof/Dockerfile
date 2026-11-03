ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
FROM ${DOTNET_SDK_IMAGE} AS build
WORKDIR /src
COPY src/FuelTrack.Domain/FuelTrack.Domain.csproj src/FuelTrack.Domain/
COPY src/FuelTrack.Application/FuelTrack.Application.csproj src/FuelTrack.Application/
COPY src/FuelTrack.Infrastructure/FuelTrack.Infrastructure.csproj src/FuelTrack.Infrastructure/
COPY src/FuelTrack.Platform/FuelTrack.Platform.csproj src/FuelTrack.Platform/
COPY src/FuelTrack.Admin/FuelTrack.Admin.csproj src/FuelTrack.Admin/
COPY src/FuelTrack.API/FuelTrack.API.csproj src/FuelTrack.API/
COPY tests/PostgresModel.Proof/PostgresModel.Proof.csproj tests/PostgresModel.Proof/
ARG NUGET_SOURCE=https://api.nuget.org/v3/index.json
RUN dotnet restore tests/PostgresModel.Proof/PostgresModel.Proof.csproj --source "$NUGET_SOURCE"
COPY src/FuelTrack.Domain/ src/FuelTrack.Domain/
COPY src/FuelTrack.Application/ src/FuelTrack.Application/
COPY src/FuelTrack.Infrastructure/ src/FuelTrack.Infrastructure/
COPY src/FuelTrack.Platform/ src/FuelTrack.Platform/
COPY src/FuelTrack.Admin/ src/FuelTrack.Admin/
COPY src/FuelTrack.API/ src/FuelTrack.API/
COPY tests/PostgresModel.Proof/Program.cs tests/PostgresModel.Proof/SignalRIsolationProof.cs tests/PostgresModel.Proof/ApiIsolationProof.cs tests/PostgresModel.Proof/
RUN dotnet publish src/FuelTrack.API/FuelTrack.API.csproj --no-restore -c Release -o /api /p:UseAppHost=false
RUN dotnet publish tests/PostgresModel.Proof/PostgresModel.Proof.csproj --no-restore -c Release -o /app /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
COPY --from=build /api /api
USER $APP_UID
ENTRYPOINT ["dotnet", "PostgresModel.Proof.dll"]
