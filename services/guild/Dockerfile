FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY Guild.slnx .
COPY src/Guild.Domain/Guild.Domain.csproj src/Guild.Domain/
COPY src/Guild.Application/Guild.Application.csproj src/Guild.Application/
COPY src/Guild.Infrastructure/Guild.Infrastructure.csproj src/Guild.Infrastructure/
COPY src/Guild.Persistence/Guild.Persistence.csproj src/Guild.Persistence/
COPY src/Guild.Presentation/Guild.Presentation.csproj src/Guild.Presentation/

RUN dotnet restore

COPY src/ src/

RUN dotnet publish src/Guild.Presentation/Guild.Presentation.csproj \
    -c Release \
    -o /out \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /out .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Guild.Presentation.dll"]
