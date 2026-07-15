FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FuelTrack.API.sln ./
COPY src/FuelTrack.API/*.csproj src/FuelTrack.API/
COPY src/FuelTrack.Application/*.csproj src/FuelTrack.Application/
COPY src/FuelTrack.Domain/*.csproj src/FuelTrack.Domain/
COPY src/FuelTrack.Infrastructure/*.csproj src/FuelTrack.Infrastructure/
RUN dotnet restore src/FuelTrack.API/FuelTrack.API.csproj

COPY . .
RUN dotnet publish src/FuelTrack.API/FuelTrack.API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app .
ENTRYPOINT ["dotnet", "FuelTrack.API.dll"]
