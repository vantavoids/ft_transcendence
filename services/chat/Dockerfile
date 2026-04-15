FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY Chat.slnx ./
COPY Directory.Build.props ./
COPY src/Chat.Domain/Chat.Domain.csproj src/Chat.Domain/
COPY src/Chat.Application/Chat.Application.csproj src/Chat.Application/
COPY src/Chat.Infrastructure/Chat.Infrastructure.csproj src/Chat.Infrastructure/
COPY src/Chat.Persistence/Chat.Persistence.csproj src/Chat.Persistence/
COPY src/Chat.Presentation/Chat.Presentation.csproj src/Chat.Presentation/

RUN dotnet restore src/Chat.Presentation/Chat.Presentation.csproj

COPY src/ ./src/

RUN dotnet publish src/Chat.Presentation/Chat.Presentation.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Chat.Presentation.dll"]
