ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
FROM ${DOTNET_SDK_IMAGE} AS build
WORKDIR /src
COPY src/FuelTrack.Domain/FuelTrack.Domain.csproj src/FuelTrack.Domain/
COPY src/FuelTrack.Platform/FuelTrack.Platform.csproj src/FuelTrack.Platform/
COPY src/FuelTrack.Application/FuelTrack.Application.csproj src/FuelTrack.Application/
COPY src/FuelTrack.Infrastructure/FuelTrack.Infrastructure.csproj src/FuelTrack.Infrastructure/
COPY src/FuelTrack.Admin/FuelTrack.Admin.csproj src/FuelTrack.Admin/
ARG NUGET_SOURCE=https://api.nuget.org/v3/index.json
RUN dotnet restore src/FuelTrack.Admin/FuelTrack.Admin.csproj --source "$NUGET_SOURCE"
COPY src/FuelTrack.Domain/ src/FuelTrack.Domain/
COPY src/FuelTrack.Platform/ src/FuelTrack.Platform/
COPY src/FuelTrack.Application/ src/FuelTrack.Application/
COPY src/FuelTrack.Infrastructure/ src/FuelTrack.Infrastructure/
COPY src/FuelTrack.Admin/ src/FuelTrack.Admin/
RUN dotnet publish src/FuelTrack.Admin/FuelTrack.Admin.csproj -c Release --no-restore -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "FuelTrack.Admin.dll"]
