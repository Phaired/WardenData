FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WardenData/WardenData.csproj", "WardenData/"]
RUN dotnet restore "WardenData/WardenData.csproj"
COPY . .
WORKDIR "/src/WardenData"
RUN dotnet build "WardenData.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WardenData.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WardenData.dll"]
