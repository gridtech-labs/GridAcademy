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

# Railway injects PORT at runtime; the app reads it via Environment.GetEnvironmentVariable("PORT")
EXPOSE 8080

ENTRYPOINT ["dotnet", "GridAcademy.dll"]
