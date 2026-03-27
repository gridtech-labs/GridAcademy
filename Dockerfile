# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies first (layer cache)
COPY ["GridAcademy.csproj", "."]
RUN dotnet restore "GridAcademy.csproj"

# Copy everything and publish
COPY . .
RUN dotnet publish "GridAcademy.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create uploads directory (persisted via Railway volume or ephemeral)
RUN mkdir -p /app/wwwroot/uploads

COPY --from=build /app/publish .

# Railway injects PORT env var at runtime (default 8080).
# The app reads PORT and calls serverOptions.ListenAnyIP(port) in Program.cs.
EXPOSE 8080

ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "GridAcademy.dll"]
