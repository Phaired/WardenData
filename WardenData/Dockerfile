# Base Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Disable vulnerability analysis to prevent restore crash on arm64
ENV NUGET_ENABLE_VULNERABILITY_ANALYSIS=false

COPY ["WardenData/WardenData.csproj", "WardenData/"]
RUN dotnet restore "WardenData/WardenData.csproj"
COPY . .
WORKDIR "/src/WardenData"
RUN dotnet build "WardenData.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish Stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WardenData.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final Stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WardenData.dll"]
