FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/FuelTrack.API/*.csproj src/FuelTrack.API/
COPY src/FuelTrack.Application/*.csproj src/FuelTrack.Application/
COPY src/FuelTrack.Domain/*.csproj src/FuelTrack.Domain/
COPY src/FuelTrack.Infrastructure/*.csproj src/FuelTrack.Infrastructure/
ARG NUGET_SOURCE=https://api.nuget.org/v3/index.json
RUN dotnet restore src/FuelTrack.API/FuelTrack.API.csproj --source "$NUGET_SOURCE"

COPY . .
RUN dotnet publish src/FuelTrack.API/FuelTrack.API.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app .
RUN mkdir -p /app/logs && chown -R $APP_UID:$APP_UID /app/logs
USER $APP_UID
ENTRYPOINT ["dotnet", "FuelTrack.API.dll"]
