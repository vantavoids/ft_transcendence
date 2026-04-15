# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY Auth.slnx ./
COPY Directory.Build.props ./
COPY src/Auth.Domain/Auth.Domain.csproj src/Auth.Domain/
COPY src/Auth.Application/Auth.Application.csproj src/Auth.Application/
COPY src/Auth.Infrastructure/Auth.Infrastructure.csproj src/Auth.Infrastructure/
COPY src/Auth.Persistence/Auth.Persistence.csproj src/Auth.Persistence/
COPY src/Auth.Presentation/Auth.Presentation.csproj src/Auth.Presentation/

RUN dotnet restore src/Auth.Presentation/Auth.Presentation.csproj

COPY src/ ./src/

RUN dotnet publish src/Auth.Presentation/Auth.Presentation.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "Auth.Presentation.dll"]