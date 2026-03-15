FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY core/TeamFlow.Domain/TeamFlow.Domain.csproj core/TeamFlow.Domain/
COPY core/TeamFlow.Application/TeamFlow.Application.csproj core/TeamFlow.Application/
COPY core/TeamFlow.Infrastructure/TeamFlow.Infrastructure.csproj core/TeamFlow.Infrastructure/
COPY apps/TeamFlow.BackgroundServices/TeamFlow.BackgroundServices.csproj apps/TeamFlow.BackgroundServices/

RUN dotnet restore apps/TeamFlow.BackgroundServices/TeamFlow.BackgroundServices.csproj

# Copy all source
COPY core/ core/
COPY apps/TeamFlow.BackgroundServices/ apps/TeamFlow.BackgroundServices/

RUN dotnet build apps/TeamFlow.BackgroundServices/TeamFlow.BackgroundServices.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish apps/TeamFlow.BackgroundServices/TeamFlow.BackgroundServices.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TeamFlow.BackgroundServices.dll"]
