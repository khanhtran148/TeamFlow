FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY core/TeamFlow.Domain/TeamFlow.Domain.csproj core/TeamFlow.Domain/
COPY core/TeamFlow.Application/TeamFlow.Application.csproj core/TeamFlow.Application/
COPY core/TeamFlow.Infrastructure/TeamFlow.Infrastructure.csproj core/TeamFlow.Infrastructure/
COPY apps/TeamFlow.Api/TeamFlow.Api.csproj apps/TeamFlow.Api/

RUN dotnet restore apps/TeamFlow.Api/TeamFlow.Api.csproj

# Copy all source
COPY core/ core/
COPY apps/TeamFlow.Api/ apps/TeamFlow.Api/

RUN dotnet build apps/TeamFlow.Api/TeamFlow.Api.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish apps/TeamFlow.Api/TeamFlow.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TeamFlow.Api.dll"]
