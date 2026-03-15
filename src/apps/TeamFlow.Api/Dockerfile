FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY src/core/TeamFlow.Domain/TeamFlow.Domain.csproj src/core/TeamFlow.Domain/
COPY src/core/TeamFlow.Application/TeamFlow.Application.csproj src/core/TeamFlow.Application/
COPY src/core/TeamFlow.Infrastructure/TeamFlow.Infrastructure.csproj src/core/TeamFlow.Infrastructure/
COPY src/apps/TeamFlow.Api/TeamFlow.Api.csproj src/apps/TeamFlow.Api/

RUN dotnet restore src/apps/TeamFlow.Api/TeamFlow.Api.csproj

# Copy all source
COPY src/core/ src/core/
COPY src/apps/TeamFlow.Api/ src/apps/TeamFlow.Api/

RUN dotnet build src/apps/TeamFlow.Api/TeamFlow.Api.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish src/apps/TeamFlow.Api/TeamFlow.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TeamFlow.Api.dll"]
