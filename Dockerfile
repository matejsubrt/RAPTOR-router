FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /repo

# Copy csproj files first for fast Docker layer caching
COPY src/WebAPI/WebAPI.csproj src/WebAPI/
COPY src/RAPTOR-Router/RAPTOR-Router.csproj src/RAPTOR-Router/
RUN dotnet restore src/WebAPI/WebAPI.csproj

# Copy remaining source and publish
COPY . .
RUN dotnet publish src/WebAPI/WebAPI.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos "" appuser

# Copy published binaries
COPY --from=build /app/publish .

# Copy config and required static data
COPY src/config.json .
COPY src/data/forbidden_lines ./data/forbidden_lines
COPY src/data/bike_distances ./data/bike_distances

# Ensure app directory (including data downloads) is writable by appuser
RUN mkdir -p /app/data/gtfs && chown -R appuser:appuser /app
USER appuser

EXPOSE 8080
ENTRYPOINT ["dotnet", "WebAPI.dll"]
