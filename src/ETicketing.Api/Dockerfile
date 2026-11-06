# Build context is the repository root. Project files are copied first so that the
# restore layer stays cached until a dependency changes.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY src/ETicketing.Domain/ETicketing.Domain.csproj src/ETicketing.Domain/
COPY src/ETicketing.Application/ETicketing.Application.csproj src/ETicketing.Application/
COPY src/ETicketing.Infrastructure/ETicketing.Infrastructure.csproj src/ETicketing.Infrastructure/
COPY src/ETicketing.Api/ETicketing.Api.csproj src/ETicketing.Api/
RUN dotnet restore src/ETicketing.Api/ETicketing.Api.csproj
COPY src/ src/
RUN dotnet publish src/ETicketing.Api/ETicketing.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ETicketing.Api.dll"]
