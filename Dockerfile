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

# Create non-root user and ensure /app is writable for auto-downloaded GTFS data
RUN adduser --disabled-password --gecos "" appuser
COPY --from=build /app/publish .
COPY --from=build /repo/src/WebAPI/WebAPI.csproj .
COPY --from=build /repo/src/WebAPI/config.json* .
RUN chown -R appuser:appuser /app
USER appuser

EXPOSE 8080
ENTRYPOINT ["dotnet", "WebAPI.dll"]